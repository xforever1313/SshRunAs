const string buildTarget = "build";
const string buildReleaseTarget = "build_release";
const string makeDistTarget = "make_dist";
const string nugetPackTarget = "nuget_pack";
const string buildMsiTarget = "build_msi";

string target = Argument( "target", buildTarget );

FilePath sln = new FilePath( "./SshRunAs.sln" );
DirectoryPath distFolder = MakeAbsolute( new DirectoryPath( "./dist" ) );
DirectoryPath installFolder = MakeAbsolute( new DirectoryPath( "./Install" ) );
DirectoryPath wixDir = installFolder.Combine( Directory( "WiX" ) );
DirectoryPath msiWorkDir = wixDir.Combine( Directory( "obj" ) );
DirectoryPath msiOutputDir = wixDir.Combine( Directory( "bin" ) );

// This is the version of this software,
// update before making a new release.
const string version = "1.3.0";

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

        NuGetPackSettings settings = new NuGetPackSettings
        {
            Version = version,
            BasePath = distFolder,
            OutputDirectory = distFolder,
            Symbols = false,
            NoPackageAnalysis = false,
            Files = files
        };

        NuGetPack( "./nuspec/SshRunAs.nuspec", settings );
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
            OutputFile = msiOutputDir.CombineWithFilePath( $"SshRunAs.msi" ),
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
            lightSettings.OutputFile.ToString() + ".sha256",
            hashStr
        );
        Information( "Hash for " + lightSettings.OutputFile.GetFilename() + ": " + hashStr );
    }
).Description( "Builds the Windows MSI.  This requires WiX to be installed" )
.IsDependentOn( makeDistTarget );

RunTarget( target );