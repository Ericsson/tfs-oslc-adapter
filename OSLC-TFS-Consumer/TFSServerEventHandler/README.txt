The project is based on http://geekswithblogs.net/jakob/archive/2010/10/27/devleoping-and-debugging-server-side-event-handlers-in-tfs-2010.aspx

Some notes wrt debugging. If setup as described in article above, the code will be built to plugin dir and auto-deployed
So to start debug:

1. Rebuild
To make sure the new code is picked up, check the Event Viewer under Windows Logs, Application.
Should be an entry with id 9002 saying:
	The application is being shutdown for the following reason: BinDirChangeOrDirectoryRename.

Note: If the dlls for handling TFS workitems are not yet loaded in the server (e.g. you have not viewed a bug etc), there will be no restart as not needed.

2. Start Debug
Under the DEBUG menu, select Attach to Process ... and then the w3wp.exe process


Note: I had issues with multiple w3wp.exe processes and symbols not found, breakpoint disabled. But seems
as long as new build is picked up (see 1), when code is loaded breakpoints will be hit. The .pdb is located
per default beside the .dll so found by system.


Logging

The NuGet log4net (see http://www.nuget.org/packages/log4net) for logging using the NuGet Package Manager could be used,
but for now using a simpler solution based on passing info to the Windows event log.
