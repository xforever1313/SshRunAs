//
//          Copyright Seth Hendrick 2019-2024.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Diagnostics;
using System.IO;
using SethCS.Basic;

namespace SshRunAs
{
    public class LockFileManager
    {
        // ---------------- Fields ----------------

        private readonly SshConfig sshConfig;
        private readonly GenericLogger logger;

        // ---------------- Constructor ----------------

        public LockFileManager( SshConfig sshConfig, GenericLogger logger )
        {
            this.sshConfig = sshConfig;
            this.logger = logger;
        }

        // ---------------- Functions ----------------

        public void CreateLockFile()
        {
            if( string.IsNullOrEmpty( this.sshConfig.LockFile ) )
            {
                this.logger.WarningWriteLine( 2, "Lockfile not specified, not creating one" );
                return;
            }
            else if( File.Exists( this.sshConfig.LockFile ) )
            {
                throw new LockFileExistsException(
                    this.sshConfig.LockFile
                );
            }

            this.logger.WarningWriteLine( 1, $"Creating lockfile at '{this.sshConfig.LockFile}).'" );
            File.WriteAllText( this.sshConfig.LockFile, Process.GetCurrentProcess().Id.ToString() );
            this.logger.WarningWriteLine( 1, "Lockfile created!" );
        }

        public void DeleteLockFile()
        {
            if( string.IsNullOrEmpty( this.sshConfig.LockFile ) )
            {
                this.logger.WarningWriteLine( 2, "Lockfile not specified, not deleting one" );
                return;
            }
            else if( File.Exists( this.sshConfig.LockFile ) == false )
            {
                this.logger.WarningWriteLine( "Lockfile specified, but does not exist, can not delete." );
                return;
            }
            else
            {
                this.logger.WarningWriteLine( 1, "Deleting lockfile" );
                File.Delete( this.sshConfig.LockFile );
            }
        }
    }

    public class LockFileExistsException : Exception
    {
        // ---------------- Constructor ----------------

        public LockFileExistsException( string lockFilePath ) :
            base( $"Lockfile at '{lockFilePath}' exists.  Command will not be run." )
        {
        }
    }
}
