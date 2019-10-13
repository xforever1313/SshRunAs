SshRunAs
===============
SshRunAs allows one to run a command on a remote or local machine as a different user via an SSH connection.  This is a front-end to the [SSH.Net](https://github.com/sshnet/SSH.NET) Library.

The advantage of SshRunAs is unlike OpenSSH, you can pass in a raw password to this program.

Use Cases
-----
The main use case for this is to run a single command on a server via SSH.  OpenSSH can do this, but you are not allowed to pass in a password through the command line.  This bypasses that requirement, you can indirectly pass a username and password into SshRunAs.  

Honestly, if you need to use this, you should reconsider all other options, such as using SSH Keys.  This is really a last resort when things such as weird corporate IT policies get in the way.

When not to use
-----
This should not be used when:
 * You can use SSH Keys (Just use OpenSSH in that case).
 * Someone can view your process's environment variables (Passwords are stored in plaintext there).  Typically a user needs root or admin to do this anyways.
 * You are on Linux.  Linux has a better tool called [SshPass](https://linux.die.net/man/1/sshpass), use that instead.


Usage
-----
1. Save the username of the user you want to run the task as into an environment variable.
2. Save the password of that user into a different environment variable.
3. Invoke SshRunAs and pass in the names of the ENVIRONMENT VARIABLES to comamnd line.

For example (in Windows BATCH):

```bat
set USER=me
set PASSWD=SuperSecretPassword
.\SshRunAs.exe -c "curl https://shendrick.net" -u USER -p PASSWD -s myserver.local
```

Install
-----
You can install this right through Nuget.  At the moment, only the Windows version is posted to NuGet since Unix has a better alternative called [SshPass](https://linux.die.net/man/1/sshpass).
