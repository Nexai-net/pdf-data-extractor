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
A file **document.json** is generate. This file is serialized by .net json serializer.

By default the images are extract in a sub folder, with the uid as name.
If you select the option "--IncludeImages" the image will be serialized in base64 in the json document.

**PDF.Data.Extractor.Console.exe --help** <br />
Copyright (C) 2024 PDF.Data.Extractor.Console

  **-s, --source** Pdf file path to extract

  **-o, --output**        Required. Director path where all the datablock will be
                      extract.

  **-f, --force**         (Default: false) Override the ouput if already exists

  **--IncludeImages**     (Default: false) If set to true the image content will be
                      integrated in the result json in bas64

  **--help**              Display this help screen.

  **--version**           Display version information.


# Viewer

The viewer [/src/PDF.Data.Extractor.Viewer/](/src/PDF.Data.Extractor.Viewer/) is a basic way to look graphically the data extracted.

# Using

We advise to use the console line to extract information before using them in you solutions <br/>
A package **'Data.Block.Abstractions'** is available on nuget to facilitate the json deserialization.