using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Chimera.Emulation.Common.Waterbox
{
	/// <summary>
	/// The optional half of the guest ABI: CORE-MANAGED TOOLING. A core may export
	/// any subset of four independent groups - probed and driven by the engine's
	/// session - and the adapter exposes exactly the services the groups it finds
	/// can back:
	///
	/// <list type="bullet">
	/// <item>surfaces  -> <see cref="ICoreSurfaces"/> (core-rendered viewer windows)</item>
	/// <item>registers -> <see cref="IDebuggable"/> (the generic debugger's register box)</item>
	/// <item>buses     -> extra <see cref="IMemoryDomains"/> entries (peek/poke address spaces)</item>
	/// <item>trace     -> <see cref="ITraceable"/> (the trace logger)</item>
	/// <item>savedata  -> <see cref="ICoreSaveData"/> (Emulator > Export Save Data...)</item>
	/// </list>
	///
	/// Nothing here is core-specific: the frontend never learns what a nametable or
	/// a 6502 is. Absent exports simply mean the tool is unavailable for that core,
	/// which the service provider communicates by not registering the service.
	/// </summary>
	public sealed partial class WaterboxCore : IDebuggable, ITraceable, ICoreSurfaces, ICoreSaveData, IMemoryCallbackReplaceable
	{
		private string[] _surfaceNames = [ ];
		private int[][] _surfaceBuffs = [ ];
		private string[] _regNames = [ ];
		private ITraceSink? _traceSink;
		private bool _traceOverflowed;
		private MemoryCallbackSystem? _memoryCallbacks;
		private string[] _memoryScopes = [ ];

		/// <summary>
		/// Reads what the session probed after Init, and unregisters the services no
		/// group could back. The services are implemented unconditionally on this
		/// type, so <see cref="BasicServiceProvider"/> would otherwise advertise all
		/// of them for every core.
		/// </summary>
		private void InitTooling(BasicServiceProvider services, List<MemoryDomain> domains)
		{
			int surfaces = _session.SurfaceCount;
			_surfaceNames = new string[surfaces];
			_surfaceBuffs = new int[surfaces][];
			for (int i = 0; i < surfaces; i++)
			{
				_surfaceNames[i] = _session.SurfaceName(i);
				_surfaceBuffs[i] = new int[_session.SurfaceWidth(i) * _session.SurfaceHeight(i)];
			}

			_regNames = new string[_session.RegisterCount];
			for (int i = 0; i < _regNames.Length; i++) _regNames[i] = _session.RegisterName(i);

			// Buses are address SPACES rather than blocks of memory - the guest
			// resolves each access through its own mapper/mirroring logic - so they
			// join the domain list as callback-backed domains, which is what makes
			// them show up in the hex editor and the watch tools with no plumbing.
			for (int i = 0; i < _session.BusCount; i++)
			{
				int bus = i; // capture per iteration
				bool writable = _session.BusWritable(i);
				domains.Add(new MemoryDomainDelegate(
					_session.BusName(i),
					_session.BusSize(i),
					MemoryDomain.Endian.Little,
					// guarded: a domain can outlive the core in a tool that kept a reference
					addr => _session.Disposed ? (byte)0 : _session.BusPeek(bus, (int)addr),
					!writable ? null : (addr, val) => { if (!_session.Disposed) _session.BusPoke(bus, (int)addr, val); },
					1,
					// a run in one call: RAM Search reads the whole bus, and a peek per
					// byte was a crossing into the guest per byte - seconds of them over
					// a 64 MB bus (chimera#180)
					bulkPeekByte: (range, values) =>
					{
						if (_session.Disposed) Array.Clear(values, 0, values.Length);
						else _session.BusRead(bus, range.Start, values);
					})
				{
					// a bus over an address space that is mostly nothing (chimera#218)
					LiveRanges = () => _session.Disposed ? null : _session.BusRanges(bus),
				});
			}

			if (surfaces is 0) services.Unregister<ICoreSurfaces>();
			SetUpMemoryCallbacks();
			// the debugging service is the registers AND the memory callbacks: a
			// core with either keeps it, and a core with neither does not advertise it
			if (_regNames.Length is 0 && _memoryCallbacks is null) services.Unregister<IDebuggable>();
			if (!_session.TraceAvailable) services.Unregister<ITraceable>();
			if (!_session.SaveDataAvailable) services.Unregister<ICoreSaveData>();
		}

		// ---------------- ICoreSurfaces ----------------

		public IReadOnlyList<string> SurfaceNames => _surfaceNames;

		public void GetSurfaceSize(int index, out int width, out int height)
		{
			width = _session.SurfaceWidth(index);
			height = _session.SurfaceHeight(index);
		}

		public int[] RenderSurface(int index)
		{
			CheckDisposed();
			if (_surfaceNames.Length is 0) throw new NotImplementedException($"{_cfg.CoreName} exposes no surfaces");
			var buff = _surfaceBuffs[index];
			var p = _session.RenderSurface(index);
			if (p != IntPtr.Zero) Marshal.Copy(p, buff, 0, buff.Length);
			return buff;
		}

		// ---------------- IDebuggable ----------------

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			CheckDisposed();
			if (_regNames.Length is 0) throw new NotImplementedException($"{_cfg.CoreName} exposes no registers");
			var dict = new Dictionary<string, RegisterValue>(_regNames.Length);
			for (int i = 0; i < _regNames.Length; i++)
			{
				dict[_regNames[i]] = new RegisterValue((ulong)_session.RegisterValue(i), (byte)_session.RegisterBits(i));
			}
			return dict;
		}

		public void SetCpuRegister(string register, int value)
		{
			CheckDisposed();
			for (int i = 0; i < _regNames.Length; i++)
			{
				if (_regNames[i] == register)
				{
					if (!_session.SetRegister(i, value))
					{
						throw new NotImplementedException($"{_cfg.CoreName} does not support writing registers");
					}
					return;
				}
			}
			throw new InvalidOperationException($"no such register: {register}");
		}

		// Breakpoints and stepping would need the guest to give up control mid-frame,
		// which the waterbox ABI (one FrameAdvance call, no re-entry) doesn't allow.
		// Memory callbacks do not: a core that offers the four exports has the
		// engine call at each access it watches (issues 496 and 534).
		public IMemoryCallbackSystem MemoryCallbacks
#pragma warning disable CA1065 // the convention for a feature a core lacks is to throw NIE
			=> _memoryCallbacks ?? throw new NotImplementedException($"{_cfg.CoreName} does not watch memory");
#pragma warning restore CA1065

		/// <summary>Which kinds' values this core lets a callback change (issue 534); 0 when it only reports.</summary>
		public int ReplaceableMemoryKinds => _session.MemoryReplaceable;

		/// <summary>Looks at the engine's scopes once the session is open; a core without them keeps no system.</summary>
		private void SetUpMemoryCallbacks()
		{
			var scopes = _session.MemoryScopes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			if (scopes.Length is 0) return;
			var callbacks = new MemoryCallbackSystem(scopes);
			callbacks.CallbackAdded += callback => Watch(callback, true);
			callbacks.CallbackRemoved += callback => Watch(callback, false);
			_memoryScopes = scopes;
			_memoryCallbacks = callbacks;
			_session.SetMemoryCallback(OnMemoryAccess);
		}

		/// <summary>Tells the core to watch (or stop watching) one callback's place; the engine resolves it.</summary>
		private void Watch(IMemoryCallback callback, bool on)
		{
			var kind = callback.Type switch
			{
				MemoryCallbackType.Execute => 0,
				MemoryCallbackType.Read => 1,
				MemoryCallbackType.Write => 2,
				_ => -1,
			};
			var scope = Array.IndexOf(_memoryScopes, callback.Scope);
			if (kind < 0 || scope < 0)
			{
				if (on) throw new InvalidOperationException($"{_cfg.CoreName} has no {callback.Scope} to watch");
				return;
			}
			var address = callback.Address ?? ulong.MaxValue; // the engine's CE_ANY_ADDRESS
			var took = _session.WatchMemory(kind, scope, address, on);
			if (!on || took) return;
			// the core refused: take the callback off the list before refusing the
			// script, so what is registered is what will be called
			_memoryCallbacks?.Remove(callback.Callback);
			throw new InvalidOperationException($"{_cfg.CoreName} cannot watch {callback.Scope} for {callback.Type}");
		}

		/// <summary>Engine's call at a watched access, on the advancing thread; gives the value to use, if changed.</summary>
		private ulong? OnMemoryAccess(int kind, int scope, ulong address, ulong value, int size)
		{
			var callbacks = _memoryCallbacks;
			if (callbacks is null) return null;
			uint flags = kind switch
			{
				0 => (uint)MemoryCallbackFlags.AccessExecute,
				1 => (uint)MemoryCallbackFlags.AccessRead,
				2 => (uint)MemoryCallbackFlags.AccessWrite,
				_ => 0,
			};
			flags |= size switch
			{
				1 => (uint)MemoryCallbackFlags.SizeByte,
				2 => (uint)MemoryCallbackFlags.SizeWord,
				4 => (uint)MemoryCallbackFlags.SizeLong,
				_ => 0,
			};
			var scopeName = scope >= 0 && scope < _memoryScopes.Length ? _memoryScopes[scope] : _memoryScopes[0];
			var result = callbacks.CallMemoryCallbacks((uint)address, (uint)value, flags, scopeName);
			return result != (uint)value ? result : null;
		}

		public bool CanStep(StepType type) => false;

		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles
			=> _session.HasExecutedCycles ? _session.ExecutedCycles : throw new NotImplementedException($"{_cfg.CoreName} does not report a cycle count");

		// ---------------- ICoreSaveData ----------------
		// Pure forwarding: the session snapshots the guest's dynamic file list
		// and streams bytes out in ranged reads (docs/save-data.md).

		public int SaveDataSnapshot()
		{
			CheckDisposed();
			return _session.SaveDataSnapshot();
		}

		public string SaveDataName(int index) => _session.SaveDataName(index);

		public long SaveDataSize(int index) => _session.SaveDataSize(index);

		public int SaveDataRead(int index, long offset, byte[] buffer) => _session.SaveDataRead(index, offset, buffer);

		// ---------------- ITraceable ----------------

		public string Header => _session.TraceHeader;

		/// <summary>
		/// Attaching a sink turns tracing ON inside the guest (the session remembers
		/// the flag and re-asserts it after a state load). The guest appends lines to
		/// a buffer of its own and we drain it once per frame - a callback per
		/// instruction would cross the sandbox boundary millions of times a second.
		/// </summary>
		public ITraceSink? Sink
		{
			get => _traceSink;
			set
			{
				bool toggling = (_traceSink != null) != (value != null);
				_traceSink = value;
				if (toggling && _session.TraceAvailable) _session.TraceEnable(value != null);
			}
		}

		private void DrainTrace()
		{
			var sink = _traceSink;
			if (sink == null || !_session.TraceAvailable) return;

			var bytes = _session.TraceDrain(out _, out var overflowed);
			int start = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				if (bytes[i] != 0) continue;
				PutTraceLine(sink, Encoding.ASCII.GetString(bytes, start, i - start));
				start = i + 1;
			}

			// Report a truncated frame once rather than every frame it recurs.
			if (overflowed && !_traceOverflowed)
			{
				_traceOverflowed = true;
				sink.Put(new TraceInfo($"[{_cfg.CoreName}: trace buffer full, lines dropped]", ""));
			}
		}

		/// <summary>
		/// The guest writes one NUL-terminated line per instruction, optionally split
		/// by a tab into the disassembly and the register state - the two columns the
		/// trace logger shows. Cores that don't split get one wide column.
		/// </summary>
		private static void PutTraceLine(ITraceSink sink, string line)
		{
			int tab = line.IndexOf('\t');
			sink.Put(tab < 0
				? new TraceInfo(line, "")
				: new TraceInfo(line.Substring(0, tab), line.Substring(tab + 1)));
		}
	}
}
