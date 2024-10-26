//
//          Copyright Seth Hendrick 2019-2024.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Mono.Options;
using SethCS.Basic;
using SethCS.Exceptions;
using SethCS.IO;

namespace SshRunAs
{
    class Program
    {
        // ---------------- Fields ----------------

        private static bool noColor = false;

        // ---------------- Constructor ----------------

        static int Main( string[] args )
        {
            try
            {
                bool showHelp = false;
                bool showVersion = false;
                bool showLicense = false;
                bool showCredits = false;
                bool showReadme = false;

                bool dryRun = false;

                int verbosity = 0;

                SshConfig defaultConfig = new SshConfig();
                SshConfig actualConfig = new SshConfig();

                OptionSet options = new OptionSet
                {
                    {
                        "h|help",
                        "Shows this message and exits.",
                        v => showHelp = ( v != null )
                    },
                    {
                        "version",
                        "Shows the version and exits.",
                        v => showVersion = ( v != null )
                    },
                    {
                        "license",
                        "Shows the license information and exits.",
                        v => showLicense = ( v != null )
                    },
                    {
                        "readme",
                        "Shows the readme as markdown and exits.",
                        v => showReadme = ( v != null )
                    },
                    {
                        "dryrun|dry_run|whatif|noop",
                        "If specified, no action is taking other than printing the command to stdout.",
                        v => dryRun = ( v != null )
                    },
                    {
                        "no_color|nocolor",
                        $"If specified, {nameof( SshRunAs )} will not change the console's colors.",
                        v => noColor = ( v != null )
                    },
                    {
                        "credits",
                        "Shows the third-party credits information as markdown and exits.",
                        v => showCredits = ( v != null )
                    },
                    {
                        "c=|command=",
                        $"The command to pass into the SSH progress. Required.",
                        v => actualConfig.Command = v
                    },
                    {
                        "u=|user=",
                        $"The name of the ENVIRONMENTAL VARIABLE that contains the user name.  This is NOT the username itself.  Required",
                        v => actualConfig.UserNameEnvVarName = v
                    },
                    {
                        "p=|pass_env=",
                        $"The name of the ENVIRONMENTAL VARIABLE that contains the password.  This is NOT the password itself.  Required.",
                        v => actualConfig.PasswordEnvVarName = v
                    },
                    {
                        "s=|server=",
                        $"The host or IP of the server to connect to.  Required.",
                        v => actualConfig.Server = v
                    },
                    {
                        "v=|verbosity=",
                        $"The verbosity of the output.  0 for no messages from {nameof( SshRunAs )}.  Defaulted to 0.",
                        v =>
                        {
                            if( int.TryParse( v, out verbosity ) == false )
                            {
                                throw new ArgumentException( "Verbosity must be an integer" );
                            }
                        }
                    },
                    {
                        "f=|lock_file=",
                        $"Where to create a 'lock file'.  If this file exists, {nameof( SshRunAs )} will not execute.  " +
                            $"When the command completes *OR* CTRL+C is sent to cancel, the lock file will be deleted.  " +
                            $"If CTRL+BREAK is sent, the lock file will NOT be deleted.",
                        v =>
                        {
                            actualConfig.LockFile = v;
                        }
                    },
                    {
                        "P=|port=",
                        $"The port to connect to.  Defaulted to {defaultConfig.Port}.",
                        v =>
                        {
                            if( ushort.TryParse( v, out ushort port ) )
                            {
                                actualConfig.Port = port;
                            }
                            else
                            {
                                throw new ArgumentException( "Port must be an unsigned int" );
                            }
                        }
                    }
                };

                options.Parse( args );

                if( showHelp )
                {
                    Console.WriteLine( "Usage:  SshRunAs -s server -u userEnvVar -p passwordEnvVar -c command [-P port]" );
                    options.WriteOptionDescriptions( Console.Out );
                    Console.WriteLine();
                    Console.WriteLine( "Exiting early:" );
                    Console.WriteLine( "- Hit CTRL+C to stop the connection, and attempt to cleanup." );
                    Console.WriteLine( "- Hit CTRL+BREAK to exit right away with no attempt to cleanup." );
                    Console.WriteLine();
                    Console.WriteLine( "Have an issue? Need more help? File an issue: https://github.com/xforever1313/SshRunAs" );
                }
                else if( showVersion )
                {
                    ShowVersion();
                }
                else if( showLicense )
                {
                    ShowLicense();
                }
                else if( showReadme )
                {
                    ShowReadme();
                }
                else if( showCredits )
                {
                    ShowCredits();
                }
                else
                {
                    using( CancellationTokenSource cancelToken = new CancellationTokenSource() )
                    {
                        GenericLogger logger = new GenericLogger( verbosity );

                        ConsoleCancelEventHandler onCtrlC = delegate ( object sender, ConsoleCancelEventArgs cancelArgs )
                        {
                            // Wait for the process to end gracefully if we get CTRL+C,
                            // otherwise, let it die without clean up if we get CTRL+Break.
                            if( cancelArgs.SpecialKey == ConsoleSpecialKey.ControlC )
                            {
                                try
                                {
                                    cancelArgs.Cancel = true;
                                    logger.WarningWriteLine( "CTRL+C was received, stopping SSH connection..." );
                                    cancelToken.Cancel();
                                }
                                catch( Exception e )
                                {
                                    logger.ErrorWriteLine( "Caught exception when cancelling: " + Environment.NewLine + e.ToString() );
                                }
                            }
                            else
                            {
                                logger.WarningWriteLine( "CTRL+BREAK was received, killing process..." );
                            }
                        };

                        try
                        {
                            Console.CancelKeyPress += onCtrlC;
                            logger.OnWriteLine += Logger_OnWriteLine;
                            logger.OnErrorWriteLine += Logger_OnErrorWriteLine;
                            logger.OnWarningWriteLine += Logger_OnWarningWriteLine;

                            logger.WarningWriteLine( $"Running '{actualConfig.Command}' using password stored in '{actualConfig.PasswordEnvVarName}' on {actualConfig.Server}:{actualConfig.Port}" );

                            if( dryRun == false )
                            {
                                using( SshRunner runner = new SshRunner( actualConfig, logger ) )
                                {
                                    SshResult result = runner.RunSsh( cancelToken.Token );
                                    if( result.ExitCode.HasValue )
                                    {
                                        return result.ExitCode.Value;
                                    }
                                    else if( string.IsNullOrWhiteSpace( result.ExitSignal ) == false )
                                    {
                                        logger.ErrorWriteLine(
                                            $"SSH process terminated violently for reason: {result.ExitSignal}"
                                        );
                                        return 20;
                                    }
                                    else
                                    {
                                        logger.ErrorWriteLine(
                                            "SSH Process exited with no exit code or exit signal"
                                        );
                                        return 21;
                                    }
                                }
                            }
                            else
                            {
                                logger.WarningWriteLine( "Dry run set, no action taken." );
                            }
                        }
                        finally
                        {
                            Console.CancelKeyPress -= onCtrlC;
                            logger.OnWriteLine -= Logger_OnWriteLine;
                            logger.OnErrorWriteLine -= Logger_OnErrorWriteLine;
                            logger.OnWarningWriteLine -= Logger_OnWarningWriteLine;
                        }
                    }
                }

                return 0;
            }
            catch( OptionException e )
            {
                using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
                {
                    Console.WriteLine( "Invalid Arguments: " + e.Message );
                    Console.WriteLine();
                }

                return 14;
            }
            catch( ArgumentException e )
            {
                using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
                {
                    Console.WriteLine( "Invalid Arguments: " + e.Message );
                    Console.WriteLine();
                }

                return 14;
            }
            catch( ListedValidationException e )
            {
                using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
                {
                    Console.WriteLine( "Invalid Arguments: " + Environment.NewLine + e.Message );
                    Console.WriteLine();
                }

                return 14;
            }
            catch( OperationCanceledException e )
            {
                Console.WriteLine( e.Message );
                return 15;
            }
            catch( LockFileExistsException e )
            {
                using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
                {
                    Console.WriteLine( e.Message );
                    return 16;
                }
            }
            catch( Exception e )
            {
                using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
                {
                    Console.WriteLine( "Unexpected Exception: " );
                    Console.WriteLine( e.Message );
                }
                return 13;
            }
        }

        private static void Logger_OnWarningWriteLine( string obj )
        {
            using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Yellow ), null ) )
            {
                Console.Write( $"{nameof( SshRunAs )}: " + obj );
            }
        }

        private static void Logger_OnErrorWriteLine( string obj )
        {
            using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Red ), null ) )
            {
                Console.Error.Write( obj );
            }
        }

        private static void Logger_OnWriteLine( string obj )
        {
            using( ConsoleColorResetter colorResetter = new ConsoleColorResetter( GetColor( ConsoleColor.Green ), null ) )
            {
                Console.Write( obj );
            }
        }

        private static void ShowLicense()
        {
            StringBuilder license = new StringBuilder();
            license.AppendLine( $"{nameof( SshRunAs ) }- Copyright Seth Hendrick 2019-2024." );
            license.AppendLine();
            license.AppendLine( ReadResource( "SshRunAs.LICENSE_1_0.txt" ) );

            Console.WriteLine( license );
        }

        private static void ShowVersion()
        {
            Console.WriteLine(
                Assembly.GetExecutingAssembly().GetName().Version
            );
        }

        private static void ShowReadme()
        {
            string readme = ReadResource( "SshRunAs.Readme.md" );
            Console.WriteLine( readme );
        }

        private static void ShowCredits()
        {
            string credits = ReadResource( "SshRunAs.Credits.md" );
            Console.WriteLine( credits );
        }

        private static string ReadResource( string resourceName )
        {
            using( Stream stream = typeof( Program ).Assembly.GetManifestResourceStream( resourceName ) )
            {
                using( StreamReader reader = new StreamReader( stream ) )
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static ConsoleColor? GetColor( ConsoleColor desiredColor )
        {
            if( noColor )
            {
                return null;
            }
            else
            {
                return desiredColor;
            }
        }
    }
}
