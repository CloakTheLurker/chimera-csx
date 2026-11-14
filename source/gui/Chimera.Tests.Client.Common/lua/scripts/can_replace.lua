print(tostring(event.can_replace_value("read")))
print(tostring(event.can_replace_value("write")))
print(tostring(event.can_replace_value("exec")))
print(tostring(pcall(event.can_replace_value, "bad")))
