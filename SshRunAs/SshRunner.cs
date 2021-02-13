//
//          Copyright Seth Hendrick 2019-2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        public int RunSsh( CancellationToken cancelToken )
        {
            this.config.Validate();

            using( SshClient client = new SshClient( this.config.Server, this.config.Port, this.config.UserName, this.config.Password ) )
            {
                client.Connect();

                using( SshCommand command = client.CreateCommand( this.config.Command ) )
                {
                    IAsyncResult task = command.BeginExecute();

                    // Using tasks seems to print things to the console better; it doesn't all just bunch up at the end.
                    Task stdOutTask = AsyncWriteToStream( Console.OpenStandardOutput, command.OutputStream, task, "STDOUT", cancelToken );
                    Task stdErrTask = AsyncWriteToStream( Console.OpenStandardError, command.ExtendedOutputStream, task, "STDERR", cancelToken );

                    try
                    {
                        Task[] tasks = new Task[] { stdOutTask, stdErrTask };
                        Task.WaitAll( tasks, cancelToken );
                    }
                    catch( OperationCanceledException )
                    {
                        this.logger.WarningWriteLine( 1, "Cancelling Task...  Hit CTRL+BREAK instead to exit right away." );
                        this.logger.WarningWriteLine( 2, "Cancelling Streams" );
                        command.OutputStream.Dispose();
                        command.ExtendedOutputStream.Dispose();
                        this.logger.WarningWriteLine( 2, "Cancelling Async Task" );
                        command.CancelAsync();
                        this.logger.WarningWriteLine( 2, "Ending Task Task" );
                        this.logger.WarningWriteLine( 1, "Task Cancelled" );
                        throw;
                    }

                    command.EndExecute( task );

                    int exitStatus = command.ExitStatus;

                    this.logger.WarningWriteLine( 1, "Process exited with exit code: " + exitStatus );

                    return exitStatus;
                }
            }
        }

        private Task AsyncWriteToStream( Func<Stream> oStreamAction, Stream iStream, IAsyncResult sshTask, string context, CancellationToken cancelToken  )
        {
            return Task.Run(
                async delegate()
                {
                    try
                    {
                        using( Stream oStream = oStreamAction() )
                        {
                            while( sshTask.IsCompleted == false )
                            {
                                await iStream.CopyToAsync( oStream, cancelToken );
                                await oStream.FlushAsync( cancelToken );
                                await Task.Delay( 500, cancelToken ); // So we don't burn through CPU.
                            }

                            // One more loop so we don't miss any last-minute output:
                            await iStream.CopyToAsync( oStream, cancelToken );
                            await oStream.FlushAsync( cancelToken );
                        }
                    }
                    catch( OperationCanceledException )
                    {
                        this.logger.WarningWriteLine( 1, $"{context} cancelled" );
                    }
                    catch( Exception e )
                    {
                        this.logger.ErrorWriteLine( "An output stream has failed.  Output will stop, but the task is still running: " + e.Message );
                    }
                }
            );
        }

        public void Dispose()
        {
        }
    }
}
