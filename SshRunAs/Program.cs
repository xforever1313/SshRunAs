//
//          Copyright Seth Hendrick 2019-2021.
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

namespace SshRunAs
{
    class Program
    {
        static int Main( string[] args )
        {
            try
            {
                bool showHelp = false;
                bool showVersion = false;
                bool showLicense = false;
                bool showCredits = false;
                bool showReadme = false;

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
                        $"The verbosity of the output.  0 for no messages from SshRunAs.  Defaulted to 0.",
                        v =>
                        {
                            if( int.TryParse( v, out verbosity ) == false )
                            {
                                throw new ArgumentException( "Verbosity must be an integer" );
                            }
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
                                cancelArgs.Cancel = true;
                                logger.WarningWriteLine( 1, "CTRL+C was received, cleaning up..." );
                                cancelToken.Cancel();
                            }
                        };

                        try
                        {
                            Console.CancelKeyPress += onCtrlC;
                            logger.OnWriteLine += Logger_OnWriteLine;
                            logger.OnErrorWriteLine += Logger_OnErrorWriteLine;
                            logger.OnWarningWriteLine += Logger_OnWarningWriteLine;

                            logger.WarningWriteLine( $"Running '{actualConfig.Command}' using password stored in '{actualConfig.PasswordEnvVarName}' on {actualConfig.Server}:{actualConfig.Port}" );

                            using( SshRunner runner = new SshRunner( actualConfig, logger ) )
                            {
                                int exitCode = runner.RunSsh( cancelToken.Token );
                                return exitCode;
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
                Console.WriteLine( "Invalid Arguments: " + e.Message );
                Console.WriteLine();

                return 1;
            }
            catch( OperationCanceledException e )
            {
                Console.WriteLine( e.Message );
                return 2;
            }
            catch( Exception e )
            {
                Console.WriteLine( "Unexpected Exception: " );
                Console.WriteLine( e.Message );

                return -1;
            }
        }

        private static void Logger_OnWarningWriteLine( string obj )
        {
            Console.Write( "SshRunAs: " + obj );
        }

        private static void Logger_OnErrorWriteLine( string obj )
        {
            Console.Error.Write( obj );
        }

        private static void Logger_OnWriteLine( string obj )
        {
            Console.Write( obj );
        }

        private static void ShowLicense()
        {
            StringBuilder license = new StringBuilder();
            license.AppendLine( "SshRunAs - Copyright Seth Hendrick 2019-2021." );
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
    }
}
