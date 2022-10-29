using System.Text.RegularExpressions;

const string buildTarget = "build";
const string buildReleaseTarget = "build_release";
const string makeDistTarget = "make_dist";
const string nugetPackTarget = "nuget_pack";
const string buildMsiTarget = "build_msi";
const string chocoPackTarget = "choco_pack";

string target = Argument( "target", buildTarget );
bool noBuild = Argument<bool>( "no_build", false );

FilePath sln = new FilePath( "./SshRunAs.sln" );
DirectoryPath distFolder = MakeAbsolute( new DirectoryPath( "./dist" ) );
DirectoryPath installFolder = MakeAbsolute( new DirectoryPath( "./Install" ) );
DirectoryPath wixDir = installFolder.Combine( Directory( "WiX" ) );
DirectoryPath msiWorkDir = wixDir.Combine( Directory( "obj" ) );
DirectoryPath msiOutputDir = wixDir.Combine( Directory( "bin" ) );
FilePath msiPath = msiOutputDir.CombineWithFilePath( "SshRunAs.msi" );
FilePath msiShaFile = File( msiPath.ToString() + ".sha256" );

// This is the version of this software,
// update before making a new release.
const string version = "3.0.1";

DotNetCoreMSBuildSettings msBuildSettings = new DotNetCoreMSBuildSettings();

// Sets the assembly version.
msBuildSettings.WithProperty( "Version", version )
    .WithProperty( "AssemblyVersion", version )
    .SetMaxCpuCount( System.Environment.ProcessorCount )
    .WithProperty( "FileVersion", version );

Task( buildTarget )
.Does(
    () =>
    {
        Build( "Debug" );
    }
).Description( "Builds the Debug target." );

Task( buildReleaseTarget )
.Does(
    () =>
    {
        Build( "Release" );
    }
).Description( "Builds with the Release Configuration." );

void Build( string config )
{
    msBuildSettings.SetConfiguration( config );
    DotNetCoreBuildSettings settings = new DotNetCoreBuildSettings
    {
        MSBuildSettings = msBuildSettings
    };
    DotNetCoreBuild( sln.ToString(), settings );
}

Task( makeDistTarget )
.Does(
    () =>
    {
        EnsureDirectoryExists( distFolder );
        CleanDirectory( distFolder );

        DotNetCorePublishSettings settings = new DotNetCorePublishSettings
        {
            OutputDirectory = distFolder,
            Configuration = "Release",
            SelfContained = true,
            Runtime = "win-x64",
            MSBuildSettings = msBuildSettings
        };

        DotNetCorePublish( "./SshRunAs/SshRunAs.csproj", settings );
        CopyFile( "./LICENSE_1_0.txt", System.IO.Path.Combine( distFolder.ToString(), "License.txt" ) );
        CopyFileToDirectory( "./Credits.md", distFolder );
        CopyFileToDirectory( "./Readme.md", distFolder );
    }
).Description( "Moves the files into directory so it can be distributed." ).
IsDependentOn( buildReleaseTarget );

Task( nugetPackTarget )
.Does(
    () =>
    {
        List<NuSpecContent> files = new List<NuSpecContent>(
            GetFiles( System.IO.Path.Combine( distFolder.ToString(), "*.dll" ) )
                .Select( file => new NuSpecContent { Source = file.ToString(), Target = "tools" } )
        );

        files.AddRange(
            GetFiles( System.IO.Path.Combine( distFolder.ToString(), "*.pdb" ) )
                .Select( file => new NuSpecContent { Source = file.ToString(), Target = "tools" } )
        );

        files.AddRange(
            GetFiles( System.IO.Path.Combine( distFolder.ToString(), "*.exe" ) )
                .Select( file => new NuSpecContent { Source = file.ToString(), Target = "tools" } )
        );

        files.Add(
            new NuSpecContent
            { 
                Source = System.IO.Path.Combine( distFolder.ToString(), "License.txt" ),
                Target = "License.txt"
            }
        );

        files.Add(
            new NuSpecContent
            { 
                Source = System.IO.Path.Combine( distFolder.ToString(), "Readme.md" ),
                Target = "Readme.md"
            }
        );

        files.Add(
            new NuSpecContent
            { 
                Source = System.IO.Path.Combine( distFolder.ToString(), "Credits.md" ),
                Target = "Credits.md"
            }
        );

        files.Add(
            new NuSpecContent
            { 
                Source = new FilePath( "../Assets/icon.png" ).ToString(),
                Target = "icon.png"
            }
        );

        NuGetPackSettings settings = new NuGetPackSettings
        {
            Id = "SshRunAs-Win-x64",
            Version = version,
            Title = "SshRunAs",
            Authors = new string[] { "Seth Hendrick" },
            Description = "Allows one to run commands via SSH automatically while specifying a username/password.",
            Summary = "Allows one to run commands via SSH automatically while specifying a username/password.",
            License = new NuSpecLicense
            {
                Type = "expression",
                Value = "BSL-1.0"
            },
            ProjectUrl = new Uri( "https://github.com/xforever1313/SshRunAs" ),
            RequireLicenseAcceptance = false,
            Repository = new NuGetRepository
            {
                Type = "git",
                Url = "https://github.com/xforever1313/SshRunAs.git"
            },
            Copyright = "Copyright (c) Seth Hendrick",
            Tags = new string[] { "sshrunas", "ssh", "runas", "password", "sshpass", "windows", "xforever1313" },
            Icon = "icon.png",

            BasePath = distFolder,
            OutputDirectory = distFolder,
            Symbols = false,
            NoPackageAnalysis = false,
            Files = files
        };

        NuGetPack( settings );
    }
).Description( "Builds the nuget package." )
.IsDependentOn( makeDistTarget );

Task( buildMsiTarget ).
Does(
    () =>
    {
        EnsureDirectoryExists( msiWorkDir );
        CleanDirectory( msiWorkDir );

        FilePath wxsFile = msiWorkDir.CombineWithFilePath( "SshRunAs.wxs" );

        // -------- Heat --------

        Information( "Starting Heat" );

        HeatSettings heatSettings = new HeatSettings
        {
            ComponentGroupName = "SshRunAs", // -cg
            Platform = "x64",
            GenerateGuid = true,               // -gg
            SuppressFragments = true,          // -sfrag
            SuppressRegistry = true,           // -sreg
            SuppressVb6Com = true,             // -svb6
            Template = WiXTemplateType.Product,// -template product
            Transform = wixDir.CombineWithFilePath( File( "SshRunAs.xslt" ) ).ToString(),
            ToolPath = @"C:\Program Files (x86)\WiX Toolset v3.11\bin\heat.exe"
        };

        WiXHeat(
            distFolder,
            wxsFile,
            WiXHarvestType.Dir,
            heatSettings
        );

        // -------- Candle --------

        Information( "Starting Candle" );

        CandleSettings candleSettings = new CandleSettings
        {
            WorkingDirectory = msiWorkDir.ToString(),
            ToolPath = @"C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe"
        };

        WiXCandle( wxsFile.ToString(), candleSettings );

        // -------- Light --------

        Information( "Starting Light" );

        LightSettings lightSettings = new LightSettings
        {
            RawArguments = $"-ext WixUIExtension -cultures:en-us -b {distFolder.ToString()}",
            OutputFile = msiPath,
            ToolPath = @"C:\Program Files (x86)\WiX Toolset v3.11\bin\light.exe"
        };

        FilePath wixObjFile = msiWorkDir.CombineWithFilePath( $"SshRunAs.wixobj" );
        WiXLight( wixObjFile.ToString(), lightSettings );

        // Create file hash of .MSI file.
        FileHash hash = CalculateFileHash(
            lightSettings.OutputFile,
            HashAlgorithm.SHA256
        );
        string hashStr = hash.ToHex();
        System.IO.File.WriteAllText(
            msiShaFile.ToString(),
            hashStr
        );
        Information( "Hash for " + lightSettings.OutputFile.GetFilename() + ": " + hashStr );
    }
).Description( "Builds the Windows MSI.  This requires WiX to be installed" )
.IsDependentOn( makeDistTarget );

var chocoTask = Task( chocoPackTarget )
.Does(
    () =>
    {
        DirectoryPath chocoDir = installFolder.Combine( "Chocolatey" );
        DirectoryPath outputDirectory = chocoDir.Combine( "bin" );
        DirectoryPath workingDirectory = chocoDir.Combine( "obj" );
        DirectoryPath toolsDirectory = workingDirectory.Combine( "tools" );

        FilePath installScript = toolsDirectory.CombineWithFilePath( "chocolateyinstall.ps1" );

        EnsureDirectoryExists( outputDirectory );
        CleanDirectory( outputDirectory );

        EnsureDirectoryExists( workingDirectory );
        CleanDirectory( workingDirectory );

        EnsureDirectoryExists( toolsDirectory );

        // First, need to set the checksum of the MSI file.
        string checksum = System.IO.File.ReadAllText( msiShaFile.ToString() ).Trim();

        // Second, need to create the install.ps1 & uninstall/ps1 files.
        string installPs1 = 
$@"
$ErrorActionPreference = 'Stop';
$toolsDir   = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""
$fileLocation      = Join-Path $toolsDir 'SshRunAs.msi'

$packageArgs = @{{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'msi'
  file          = $fileLocation
  softwareName  = 'SshRunAs*'
  checksum    = '{checksum}'
  checksumType= 'sha256'

  # MSI
  silentArgs    = ""/qn /norestart /l*v `""$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""""
  validExitCodes= @(0, 3010, 1641)
}}

Install-ChocolateyPackage @packageArgs
";
        System.IO.File.WriteAllText( installScript.ToString(), installPs1 );
        CopyFileToDirectory(
            msiPath,
            toolsDirectory
        );

        List<ChocolateyNuSpecContent> files = new List<ChocolateyNuSpecContent>();
        foreach( FilePath file in GetFiles( toolsDirectory.Combine( "*" ).ToString() ) )
        {
            files.Add(
                new ChocolateyNuSpecContent
                {
                    Source = file.ToString(),
                    Target = "tools"
                }
            );
        }

        files.Add(
            new ChocolateyNuSpecContent
            {
                Source = File( "LICENSE_1_0.txt" ).ToString(),
                Target = "License.txt"
            }
        );

        files.Add(
            new ChocolateyNuSpecContent
            {
                Source = chocoDir.CombineWithFilePath( File( "VERIFICATION.txt" ) ).ToString(),
                Target = "VERIFICATION.txt"
            }
        );

        string readmeContents = System.IO.File.ReadAllText( "Readme.md" );

        // With our checksum set, pack it!
        ChocolateyPackSettings settings = new ChocolateyPackSettings
        {
            // Package Specific Section
            Id = "sshrunas",
            Version = version,
            PackageSourceUrl = new Uri( "https://github.com/xforever1313/SshRunAs" ),
            Owners = new string[] { "Seth Hendrick" },

            // Software Specific Section
            Title = "SshRunAs (Install)",
            Authors = new string[] { "Seth Hendrick" },
            ProjectUrl = new Uri( "https://github.com/xforever1313/SshRunAs" ),
            Copyright = "Copyright © Seth Hendrick 2019-2022",
            LicenseUrl = new Uri( "https://raw.githubusercontent.com/xforever1313/SshRunAs/master/LICENSE_1_0.txt" ),
            RequireLicenseAcceptance = false,
            ProjectSourceUrl = new Uri( "https://github.com/xforever1313/SshRunAs" ),
            BugTrackerUrl = new Uri( "https://github.com/xforever1313/SshRunAs/issues" ),
            Tags = new string[] { "sshrunas", "ssh", "runas", "password", "sshpass", "windows", "admin" },
            Summary = "Run a process via SSH and a user can pass in a username/password.",
            Description = readmeContents,
            Files = files,
            IconUrl = new Uri( "https://rawcdn.githack.com/xforever1313/SshRunAs/b92587025b0ce210a7de335d931960073496de36/Assets/icon.png" ),

            // Cake-Related Section
            OutputDirectory = outputDirectory,
            WorkingDirectory = workingDirectory
        };

        ChocolateyPack(
            settings
        );
    }
).Description( "Creates the Chocolatey Package" );

if( noBuild == false )
{
    chocoTask.IsDependentOn( buildMsiTarget );
}

RunTarget( target );
