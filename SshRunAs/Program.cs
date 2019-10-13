//
//          Copyright Seth Hendrick 2019.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Reflection;
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

                int verbosity = 0;

                SshConfig defaultConfig = new SshConfig();
                SshConfig actualConfig = new SshConfig();

                OptionSet options = new OptionSet
                {
                    {
                        "h|help|/?",
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
                        $"The verbosity of the output.  0 for no messages from SshRunAs.  Defaulted to 0",
                        v =>
                        {
                            if( int.TryParse( v, out verbosity ) == false )
                            {
                                throw new ArgumentException( "Verbosity must be an integer" );
                            }
                        }
                    }
                };

                options.Parse( args );

                if ( showHelp )
                {
                    Console.WriteLine( "Usage:  SshRunAs -s server -u userEnvVar -p passwordEnvVar -c command" );
                    options.WriteOptionDescriptions( Console.Out );
                    Console.WriteLine();
                    Console.WriteLine( "Have an issue? Need more help? File an issue: https://github.com/xforever1313/SSHPass.Net" );
                }
                else if ( showVersion )
                {
                    ShowVersion();
                }
                else if ( showLicense )
                {
                    ShowLicense();
                }
                else
                {
                    GenericLogger logger = new GenericLogger( verbosity );
                    logger.OnWriteLine += Logger_OnWriteLine;
                    logger.OnErrorWriteLine += Logger_OnErrorWriteLine;
                    logger.OnWarningWriteLine += Logger_OnWarningWriteLine;

                    logger.WarningWriteLine( $"Running '{actualConfig.Command}' using password stored in '{actualConfig.PasswordEnvVarName}'" );

                    using ( SshRunner runner = new SshRunner( actualConfig, logger ) )
                    {
                        int exitCode = runner.RunSsh();

                        Console.WriteLine( "SSH Exited with exit code " + exitCode );

                        return exitCode;
                    }
                }

                return 0;
            }
            catch ( OptionException e )
            {
                Console.WriteLine( "Invalid Arguments: " + e.Message );
                Console.WriteLine();

                return 1;
            }
            catch ( Exception e )
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
            const string license =
@"SshRunAs - Copyright Seth Hendrick 2019.

Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the ""Software"") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
";
            Console.WriteLine( license );
        }

        private static void ShowVersion()
        {
            Console.WriteLine(
                Assembly.GetExecutingAssembly().GetName().Version
            );
        }
    }
}
