net stop XRFAgent
robocopy "C:\HAC\scripts\agenttemp" "C:\Program Files\XRFAgent" /E /R:6 /W:5
rmdir /s /q C:\HAC\Scripts\agenttemp
net start XRFAgent
exit