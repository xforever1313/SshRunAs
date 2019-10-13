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

        private Thread stdOutThread;
        private Thread stdErrThread;

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
                    this.stdOutThread = new Thread( () => this.StdOutThreadEntry( command ) );
                    this.stdErrThread = new Thread( () => this.StdErrThreadEntry( command ) );

                    IAsyncResult task = command.BeginExecute();

                    this.stdOutThread.Start();
                    this.stdErrThread.Start();

                    this.stdOutThread.Join();
                    this.stdErrThread.Join();

                    // For some reason, calling command.EndExecute(task) causes the application to hang..
                    // odd

                    int exitStatus = command.ExitStatus;

                    this.logger.WarningWriteLine( 1, "Process exited with exit code: " + exitStatus );

                    return exitStatus;
                }
            }
        }

        public void Dispose()
        {
            this.CancelThread( this.stdOutThread );
            this.CancelThread( this.stdErrThread );
        }

        private void StdOutThreadEntry( SshCommand command )
        {
            try
            {
                using ( StreamReader reader = new StreamReader( command.OutputStream ) )
                {
                    string line = reader.ReadLine();
                    while ( line != null )
                    {
                        this.logger.WriteLine( line );
                        line = reader.ReadLine();
                    }
                }
            }
            catch ( Exception e )
            {
                logger.WarningWriteLine( "StdOut thread Errored: " + e.Message );
            }
            finally
            {
                logger.WarningWriteLine( 1, "StdOut thread exited." );
            }
        }

        private void StdErrThreadEntry( SshCommand command )
        {
            try
            {
                using ( StreamReader reader = new StreamReader( command.ExtendedOutputStream ) )
                {
                    string line = reader.ReadLine();
                    while ( line != null )
                    {
                        this.logger.ErrorWriteLine( line );
                        line = reader.ReadLine();
                    }
                }
            }

            catch ( Exception e )
            {
                logger.WarningWriteLine( "StdErr thread Errored: " + e.Message );
            }
            finally
            {
                logger.WarningWriteLine( 1, "StdErr thread exited" );
            }
        }

        private void CancelThread( Thread thread )
        {
            if ( thread == null )
            {
                return;
            }

            if ( ( thread.ThreadState == ThreadState.Running ) )
            {
                if ( thread.Join( 500 ) == false )
                {
                    thread.Abort();
                    thread.Join( 500 );
                }
            }
        }
    }
}
