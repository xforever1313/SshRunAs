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
        private bool keepGoing;
        private readonly object keepGoingLock;

        private Thread stdOutThread;
        private Thread stdErrThread;

        // ---------------- Constructor ----------------

        public SshRunner( SshConfig config, GenericLogger logger )
        {
            this.config = config;
            this.logger = logger;

            this.keepGoing = false;
            this.keepGoingLock = new object();
        }
        

        // ---------------- Properties ----------------

        private bool KeepGoing
        {
            get
            {
                lock ( this.keepGoingLock )
                {
                    return this.keepGoing;
                }
            }
            set
            {
                lock ( this.keepGoingLock )
                {
                    this.keepGoing = value;
                }
            }
        }

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
                    // Use threads, one for stdout and stderror.
                    // We *could* just put in a while loop between the Begin and End call,
                    // but then we only read from stdout, then only read from stderr, which means
                    // we may get stderr before stdout, but won't get printed real-time.
                    // (Assuming I understand how this thing works under the hood).
                    this.stdOutThread = new Thread(
                        () => this.ThreadEntry( command.OutputStream, "stdout", Console.OpenStandardOutput )
                    );

                    this.stdErrThread = new Thread(
                        () => this.ThreadEntry( command.ExtendedOutputStream, "stderr", Console.OpenStandardError )
                    );

                    this.keepGoing = true;
                    IAsyncResult task = command.BeginExecute();

                    this.stdOutThread.Start();
                    this.stdErrThread.Start();

                    command.EndExecute( task );

                    // Stop the threads.
                    this.KeepGoing = false;

                    this.stdOutThread.Join();
                    this.stdErrThread.Join();

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

        private void ThreadEntry( Stream inputStream, string context, Func<Stream> streamFunc )
        {
            int spinCount = 0;
            try
            {
                using ( Stream outputStream = streamFunc() )
                {
                    while ( this.KeepGoing )
                    {
                        inputStream.CopyTo( outputStream );

                        // Some sanity so we don't burn through CPU.
                        Thread.Sleep( 500 );
                        ++spinCount;
                    }
                }
            }
            catch ( Exception e )
            {
                logger.WarningWriteLine( $"{context} thread Errored: " + e.Message );
            }
            finally
            {
                logger.WarningWriteLine( 1, $"{context} thread exited" );
                logger.WarningWriteLine( 2, $"{context} thread spun this many times: " + spinCount );
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
