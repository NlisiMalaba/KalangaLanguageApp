using System.Text.Json;
using Kalanga.Application.Dtos;

namespace Kalanga.Application;

internal static class ContentPackManifestCanonical
{
    public static byte[] Write(GetContentPackManifestResult manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("algorithm", manifest.Algorithm);
            WriteNullableString(writer, "category", manifest.Category);
            writer.WriteString("generatedAt", manifest.GeneratedAt.ToUniversalTime().ToString("O"));
            writer.WriteString("languageId", manifest.LanguageId.Value);
            writer.WriteStartArray("lessons");
            foreach (var lesson in manifest.Lessons.OrderBy(static item => item.LessonId.Value))
            {
                writer.WriteStartObject();
                writer.WriteStartArray("audio");
                foreach (var audio in lesson.Audio.OrderBy(static item => item.AudioRecordingId.Value))
                {
                    writer.WriteStartObject();
                    writer.WriteString("audioRecordingId", audio.AudioRecordingId.Value);
                    writer.WriteString("cdnUrl", audio.CdnUrl);
                    writer.WriteString("fileFormat", audio.FileFormat.ToString());
                    writer.WriteNumber("fileSizeBytes", audio.FileSizeBytes);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString("lessonId", lesson.LessonId.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteNullableString(writer, "level", manifest.Level?.ToString());
            writer.WriteString("name", manifest.Name);
            writer.WriteString("packId", manifest.PackId.Value);
            writer.WriteNumber("sizeBytes", manifest.SizeBytes);
            writer.WriteNumber("version", manifest.Version);
            writer.WriteEndObject();
            writer.Flush();
        }

        return stream.ToArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
            return;
        }

        writer.WriteString(name, value);
    }
}
