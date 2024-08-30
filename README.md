PDF Data Extractor
====

This solution extract information for a PDF (Text, Image, Metadata) and produce a data structure called "Data Block" from it.<br />
This application use [IText7](https://itextpdf.com/products/itext-core) to analyze the document PDF.

The extraction result could be freely be used.

# Data block

A data block is define a type (Document, page, text, image, relation ... ), an unique id and an oriented area. <br />

The extract return many blocks. 
In post process the block are group together following rule:
- Proximity
- Orientation
- Font similare
- ...

This process construct text group that work together.

An other post process try to create a relation between groups to easi page analysis.

# Command line

In command line you need to specify where is the source document and where to put the output. <br />
A file **FILE_NAME.json** is generate. This file is serialized by .net json serializer.

> [!TIP]
> Serialization Settings <br />
>
> var settings = new JsonSerializerOptions()<br />
> {<br />
>     **WriteIndented** = true,<br />
>     **PropertyNameCaseInsensitive** = true,<br />
>     **DefaultIgnoreCondition** = JsonIgnoreCondition.WhenWritingNull,<br />
>     **NumberHandling** = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals<br />
> };<br />

By default the images are extract in a sub folder, with the uid as name.
If you select the option "--IncludeImages" the image will be serialized in base64 in the json document.

**PDF.Data.Extractor.Console.exe --help** <br />
Copyright (C) 2024 PDF.Data.Extractor.Console

````SHELL
PDF.Data.Extractor.Console 1.0.0
Copyright (c) Nexai.net

  -o, --output                (Group: OUTPUT) Director path where all the
                              datablock will be extract.

  --OutputSideFiles           (Group: OUTPUT) (Default: false) The result must
                              be set side to the origin file.

  -s, --source                (Group: SOURCE) Pdf file to extract.

  --sourceDir                 (Group: SOURCE) Folder to get all pdf files to
                              extract. (Default search only on top folder)

  -r, --recursive             Couple with option 'SourceDir' to search through
                              all the sub folder.

  -n, --outputName            Directory name create with extraction result;
                              default is the pdf name without extention

  -d, --OutputFolderName      Directory name create with extraction result;
                              default is the pdf name without extention

  -f, --force                 (Default: false) Override the ouput if already
                              exists

  --IncludeImages             (Default: false) If set to true the image content
                              will be integrated in the result json in bas64

  --SkipExtractImages         (Default: false) If set to true the image will be
                              skipped.

  -t, --Timed                 (Default: false) Display computation time.

  --PreventParallelProcess    (Default: false) Define if page should be process
                              in parallel or sequential (Parallel reduce
                              processing time but cost more memory).

  --silent                    (Default: false) Only Write minimal process logs

  --maxConcurrentDocument     (Default: 0) Define number of concurrent document
                              are extract in parallel (default 0 => nb logical
                              processor / 4)

  --help                      Display this help screen.

  --version                   Display version information.
````

# Viewer

The viewer [/src/PDF.Data.Extractor.Viewer/](/src/PDF.Data.Extractor.Viewer/) is a basic way to look graphically the data extracted.

# Using

We advise to use the console line to extract information before using them in you solutions <br/>
A package **'Data.Block.Abstractions'** is available on nuget to facilitate the json deserialization.