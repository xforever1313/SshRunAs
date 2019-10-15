//
//          Copyright Seth Hendrick 2019.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.IO;
using System.Threading;
using Renci.SshNet;
using SethCS.Basic;

namespace SshRunAs
{
    /// <summary>
    /// Runs the ssh process.
    /// </summary>
    public class SshRunner : IDisposable
    {
        // ---------------- Fields ----------------

        private readonly SshConfig config;
        private readonly GenericLogger logger;

        // ---------------- Constructor ----------------

        public SshRunner( SshConfig config, GenericLogger logger )
        {
            this.config = config;
            this.logger = logger;
        }
        
        // ---------------- Properties ----------------

        // ---------------- Functions ----------------

        /// <summary>
        /// Runs the SSH process.
        /// </summary>
        /// <returns>The exit code of the process.</returns>
        public int RunSsh()
        {
            this.config.Validate();

            using ( SshClient client = new SshClient( this.config.Server, this.config.UserName, this.config.Password ) )
            {
                client.Connect();

                using ( SshCommand command = client.CreateCommand( this.config.Command ) )
                {
                    IAsyncResult task = command.BeginExecute();

                    // Polling appears to be the only thing to do when printing stuff to the console.
                    // Gross.
                    // But, something similar is literally in SSH.Net's example.  I guess they don't have events
                    // working??
                    // https://github.com/sshnet/SSH.NET/blob/7691cb0b55f5e0de8dc2ad48dd824419471ab710/src/Renci.SshNet.Tests/Classes/SshCommandTest.cs#L99
                    int spinCount = 0;
                    using ( Stream stdOut = Console.OpenStandardOutput() )
                    {
                        using ( Stream stdErr = Console.OpenStandardError() )
                        {
                            while ( task.IsCompleted == false )
                            {
                                command.OutputStream.CopyTo( stdOut );
                                command.ExtendedOutputStream.CopyTo( stdErr );
                                ++spinCount;

                                // So we don't burn through CPU.
                                Thread.Sleep( 500 );
                            }

                            // One more read so we don't miss any characters.
                            command.OutputStream.CopyTo( stdOut );
                            command.ExtendedOutputStream.CopyTo( stdErr );
                        }
                    }

                    command.EndExecute( task );

                    int exitStatus = command.ExitStatus;

                    this.logger.WarningWriteLine( 1, "Process exited with exit code: " + exitStatus );
                    this.logger.WarningWriteLine( 2, "Stream check polled this many times: " + spinCount );

                    return exitStatus;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
