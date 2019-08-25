#addin nuget:?package=Cake.Figlet&version=1.1.0
#tool "nuget:?package=GitVersion.CommandLine&version=5.0.0-beta2-61"

var target = Argument ("target", "Default");
var configuration = Argument ("configuration", "Release");
var solutionDir = System.IO.Directory.GetCurrentDirectory ();
var artifactDir = Argument ("artifactDir", "./artifacts"); // ./build.sh --target Build-Container -artifactDir="somedir"
var slnName = Argument ("slnName", "StandardRepository.PostgreSQL");
string versionInfo = null;

var covarageDirectory="./covarage";


Information (Figlet ("StandardRepository.PostgreSQL"));


Task ("Clean")
	.Does (() => {

		var settings = new DeleteDirectorySettings {
			Recursive = true,
				Force = true
		};


		if (DirectoryExists (artifactDir)) {
			CleanDirectory (artifactDir);
			DeleteDirectory (artifactDir, settings);
		}

		var binDirs = GetDirectories ("./**/bin");
		var objDirs = GetDirectories ("./**/obj");
		var testResDirs = GetDirectories ("./**/TestResults");
		CleanDirectories (binDirs);
		CleanDirectories (objDirs);
		CleanDirectories (testResDirs);
		DeleteDirectories (binDirs, settings);
		DeleteDirectories (objDirs, settings);
		DeleteDirectories (testResDirs, settings);
	   DotNetCoreClean(".");

	});



Task ("PrepareDirectories")
	.Does (() => {
		
		EnsureDirectoryExists (artifactDir);
	});

Task ("Restore")
	.Does (() => {
		DotNetCoreRestore (".");
	});
Task ("Build")
	.IsDependentOn ("Clean")
	.IsDependentOn ("PrepareDirectories")
	.IsDependentOn ("Restore")
	.Does (() => {

		var solution = GetSlnFile();
		Information ("Build solution: {0}", solution);
		var settings = new DotNetCoreBuildSettings {
			Configuration = "Release"
		};
		DotNetCoreBuild (solution.FullPath, settings);
	});

Task ("Pack")
	.Does (() => {
		var projectPath = "./Source/StandardRepository.PostgreSQL/StandardRepository.PostgreSQL.csproj";
		var settings = new DotNetCorePackSettings {
			Configuration = "Release",
			OutputDirectory = artifactDir,
			NoBuild =true,
			ArgumentCustomization = args => {
				
				return args;
			}
		};
		DotNetCorePack(projectPath, settings);
	});

Task("Run-Unit-Tests")
    .Does(() =>
{

	
    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild=true
    }; 
    settings.ArgumentCustomization = args=> 
    args.Append("/p:CollectCoverage=true")
	.Append("/p:Exclude=[StandardRepository.PostgreSQL.IntegrationTests.*]*")
	.Append("/p:Exclude=[xunit.*]*")
	.Append("/p:UseSourceLink=true")
	.Append($"/p:CoverletOutput=../../{covarageDirectory}/covarage-result-for-unit-tests.xml")
    .Append("/p:CoverletOutputFormat=opencover");
    
    var files = GetFiles("./Tests/*.UnitTests/*.csproj");
    foreach(var file in files){
        DotNetCoreTest(file.FullPath,settings);
    }
});

Task("Run-Integration-Tests")
    .Does(() =>
{

	
    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild=true
    }; 
    settings.ArgumentCustomization = args=> 
    args.Append("/p:CollectCoverage=true")
	.Append("/p:Exclude=[StandardRepository.PostgreSQL.IntegrationTests.*]*")
	.Append("/p:Exclude=[xunit.*]*")
	.Append("/p:UseSourceLink=true")
	.Append($"/p:CoverletOutput=../../{covarageDirectory}/covarage-result-for-integration-tests.xml")
    .Append("/p:CoverletOutputFormat=opencover");
    
    var files = GetFiles("./Tests/*.IntegrationTests/*.csproj");
    foreach(var file in files){
        DotNetCoreTest(file.FullPath,settings);
    }
});

Task("Push")

.Does(()=>{
	var apiKey=	EnvironmentVariable("nugetKey");
	 var settings = new DotNetCoreNuGetPushSettings
     {
         Source = "https://www.myget.org/F/ilkerhalil/api/v3/index.json",
         ApiKey =apiKey,
     };
	 
				
	 var path=  GetFiles ("./artifacts/StandardRepository.PostgreSQL.*.nupkg").First ();
	
     DotNetCoreNuGetPush(path.FullPath, settings);
});

FilePathCollection GetSrcProjectFiles () {

	return GetFiles ("./src/**/*.csproj");
}

FilePathCollection GetTestProjectFiles () {

	return GetFiles ("./test/**/*Test/*.csproj");
}

FilePath GetSlnFile () {
	return GetFiles ("./**/*.sln").First ();
}

FilePath GetMainProjectFile () {
	foreach (var project in GetSrcProjectFiles ()) {
		if (project.FullPath.EndsWith ($"StandardRepository.PostgreSQL")) {
			return project;
		}
	}
	Error ("Cannot find main project file");
	return null;
}

RunTarget (target);