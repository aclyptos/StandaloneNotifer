# StandaloneNotifer

This is the __stripped__ source code of StandaloneNotifier available on the [YoinkerDetector project page](https://yd.just-h.party/).

Due to concerns of scraping and harassment along with DDoS we have decided to remove the endpoint from the source code.

This application [reads the VRChat output log](https://github.com/aclyptos/StandaloneNotifer/blob/main/VRCX/LogWatcher.cs) via Code taken from [VRCX](https://github.com/vrcx-team/VRCX/blob/master/LogWatcher.cs). <br />
In addition to reading the output log it will connect to the [VRCX IPC Socket](https://github.com/vrcx-team/VRCX/blob/master/IPCServer.cs) to allow the user to see the "Yoinker" tag on a users profile and get notifications when a user that is on the list joins. (See [the VRCX/IPC folder in this project](https://github.com/aclyptos/StandaloneNotifer/tree/main/VRCX/IPC) to see how it works) <br />
Those are the only data sources which StandaloneNotifier accesses. 

To ensure that the server can not use the data from lookups to track players [UserIDs and Usernames are hashed](https://github.com/aclyptos/StandaloneNotifer/blob/main/Program.cs#L191).
