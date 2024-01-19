namespace PDF.Data.Extractor.Models
{
    using CommandLine;

    public sealed class ExtractCommandLineArgument
    {
        #region Properties

        [Option('o', "output", Required = true, HelpText = "Director path where all the datablock will be extract.")]
        public string? Output { get; set; }

        [Option('s', "source", Required = true, HelpText = "Pdf file to extract.")]
        public string? Source { get; set; }

        [Option('n', "outputName", Required = false, HelpText = "Directory name create with extraction result; default is the pdf name without extention")]
        public string? OutputName { get; set; }

        [Option('f', "force", Default = false, Required = false, HelpText = "Override the ouput if already exists")]
        public bool Force { get; set; }

        #endregion
    }
}
