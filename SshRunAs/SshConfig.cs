//
//          Copyright Seth Hendrick 2019-2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using SethCS.Exceptions;

namespace SshRunAs
{
    /// <summary>
    /// Configuration for running SSH.
    /// </summary>
    public class SshConfig
    {
        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructor.  Sets the configuration to default values.
        /// </summary>
        public SshConfig()
        {
            this.Command = string.Empty;
            this.UserNameEnvVarName = string.Empty;
            this.PasswordEnvVarName = string.Empty;
            this.Port = 22;
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// Arguments to pass into the SSH process.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The environment variable name that contains the password.
        /// </summary>
        public string UserNameEnvVarName { get; set; }

        public string UserName
        {
            get
            {
                return Environment.GetEnvironmentVariable( this.UserNameEnvVarName );
            }
        }

        /// <summary>
        /// The environment variable name that contains the password.
        /// </summary>
        public string PasswordEnvVarName { get; set; }

        public string Password
        { 
            get
            {
                return Environment.GetEnvironmentVariable( this.PasswordEnvVarName );
            }
        }

        /// <summary>
        /// The server to connect to.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Port to connect to.  Defaulted to 22.
        /// </summary>
        public ushort Port { get; set; }

        // ---------------- Functions ----------------

        /// <summary>
        /// Validates the config.
        /// </summary>
        public void Validate()
        {
            List<string> errors = new List<string>();

            if ( string.IsNullOrWhiteSpace( this.Command ) )
            {
                errors.Add( nameof( Command ) + " can not be null, empty, or whitespace." );
            }

            if ( string.IsNullOrWhiteSpace( this.UserNameEnvVarName ) )
            {
                errors.Add( nameof( this.UserNameEnvVarName ) + " can not be null, empty, or whitespace." );
            }
            else if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( this.UserNameEnvVarName ) ) )
            {
                errors.Add( "Given username environment variable is empty!" );
            }

            if ( string.IsNullOrWhiteSpace( this.PasswordEnvVarName ) )
            {
                errors.Add( nameof( this.PasswordEnvVarName ) + " can not be null, empty, or whitespace." );
            }
            else if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( this.PasswordEnvVarName ) ) )
            {
                errors.Add( "Given password environment variable is empty!" );
            }

            if ( string.IsNullOrWhiteSpace( this.Server ) )
            {
                errors.Add( nameof( Server ) + " can not be null, empty, or whitespace." );
            }

            if ( errors.Count != 0 )
            {
                throw new ListedValidationException( "Errors when validating " + nameof( SshConfig ), errors );
            }
        }
    }
}
