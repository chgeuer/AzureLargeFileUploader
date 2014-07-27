AzureLargeFileUploader
======================

Needed a simple one-off command line tool to upload files to Windows Azure. 

Check http://blog.geuer-pollmann.de/blog/2014/07/21/uploading-blobs-to-azure-the-robust-way/ for more details. 





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
