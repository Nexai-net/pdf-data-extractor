namespace PDF.Data.Extractor.Console.Models
{
    using CommandLine;

    public sealed class ExtractCommandLineArgument
    {
        #region Properties

        [Option('o', "output", Required = true, HelpText = "Director path where all the datablock will be extract.")]
        public string? Output { get; set; }

        [Option('s', "source", Required = true, HelpText = "Pdf file to extract.", Group = "SOURCE")]
        public string? Source { get; set; }

        [Option("sourceDir", Required = true, HelpText = "Folder to get all pdf files to extract. (Default search only on top folder)", Group = "SOURCE")]
        public string? SourceDir { get; set; }

        [Option('r', "recursive", Required = false, HelpText = "Couple with option 'SourceDir' to search through all the sub folder.")]
        public bool SourceDirRecursive { get; set; }

        [Option('n', "outputName", Required = false, HelpText = "Directory name create with extraction result; default is the pdf name without extention")]
        public string? OutputName { get; set; }

        [Option('d', "OutputFolderName", Required = false, HelpText = "Directory name create with extraction result; default is the pdf name without extention")]
        public string? OutputFolderName { get; set; }

        [Option('f', "force", Default = false, Required = false, HelpText = "Override the ouput if already exists")]
        public bool Force { get; set; }

        [Option("IncludeImages", Default = false, Required = false, HelpText = "If set to true the image content will be integrated in the result json in bas64")]
        public bool IncludeImages { get; set; }

        [Option('t', "Timed", Default = false, Required = false, HelpText = "Display computation time.")]
        public bool Timed { get; set; }

        [Option("PreventParallelProcess", Default = false, Required = false, HelpText = "Define if page should be process in parallel or sequential (Parallel reduce processing time but cost more memory).")]
        public bool PreventParallelProcess { get; set; }

        [Option("silent", Default = false, Required = false, HelpText = "Only Write minimal process logs")]
        public bool Silent { get; set; }

        #endregion
    }
}
