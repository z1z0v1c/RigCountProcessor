﻿using RigCountProcessor.Services.FileWriters;

namespace RigCountProcessor.Services.Factories;

public class FileWriterFactory : IFileWriterFactory
{
    public IFileWriter CreateFileWriter(string fileFormat, string fileLocation)
    {
        // TODO replace with ThrowIfNullOrEmpty
        if (string.IsNullOrEmpty(fileLocation))
        {
            throw new IncorrectSettingsException("Missing OutputFileLocation setting. Check appsettings.json file.");
        }

        if (fileFormat == FileFormat.Csv)
        {
            return new CsvFileWriter(fileLocation);
        }

        throw new IncorrectSettingsException("Wrong or missing OutputFileFormat setting. Check appsettings.json file.");
    }
}