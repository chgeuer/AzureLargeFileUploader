AzureLargeFileUploader
======================

Needed a simple one-off command line tool to upload files to Windows Azure. 



Wanna include the bits without complicated DLLs? Use the T4Include NuGet package and whack this into it:

```T4
<#
    RootPath    = @"https://github.com/";
    Namespace   = "notinuse";
    Includes    = new []
    {
        Include (@"chgeuer/AzureLargeFileUploader/raw/master/LargeFileUploaderUtils.cs", noOuterNamespace: true) 
    };
#>
<#@ include file="$(SolutionDir)\packages\T4Include.1.1.2\T4\IncludeWebFile.ttinclude" #>
```
