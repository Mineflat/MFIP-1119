using Spectre.Console;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Spectre.Console.Json;
using TwentyDevs.MimeTypeDetective;
using MimeSharp;
using System.IO;
using HeyRed.Mime;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Engine;
using MimeDetective.MemoryMapping;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MFIP_1119
{
    internal class Program
    {
        private static void ShowHelpMessage()
        {
            Console.WriteLine($"To use this app run:\n\t./{AppDomain.CurrentDomain.FriendlyName} [libID] [path_to_file] [path_to_magicfile] (last option only in case 5)");
            Console.WriteLine("List fo LibID:");
            Console.WriteLine("\t [1] - Mime-Detective");
            Console.WriteLine("\t [2] - TwentyDevs.MimeTypeDetective");
            Console.WriteLine("\t [3] - MimeSharp");
            Console.WriteLine("\t [4] - TikaOnDotNet");
            Console.WriteLine("\t [5] - DotNet-native methods");
            Console.WriteLine("\t[6] - HeyRed.Mime");
            Console.WriteLine("Example usage:\n\t./{AppDomain.CurrentDomain.FriendlyName} 1 ./file.txt");
        }

        private static void OnPanic(string ErrorMessage)
        {
            Console.WriteLine($"An error occured while running this app:\n{ErrorMessage}");
            ShowHelpMessage();
            Environment.Exit(1);
        }
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                OnPanic($"Передано неверное число аргументов: {args.Length}");
            }
            // Парсинг ID библиотеки, которая будет испльзоваться
            if (ushort.TryParse(args[0], out ushort libID))
            {
                // Проверка существования файла
                if (!File.Exists(args[1]))
                {
                    OnPanic($"Указанный файл не существует или не хватает прав для его чтения: {args[1]}");
                }
                // Выбор библиотеки
                switch (libID)
                {
                    // Mime-Detective
                    case 1:
                        ContentInspectorEnumeration(args[1]);
                        break;
                    // TwentyDevs.MimeTypeDetective
                    case 2:
                        GetMimeTypeDetective(args[1]);
                        break;
                    // MimeSharp
                    case 3:
                        var mime = new Mime();
                        Console.WriteLine(mime.Lookup(args[1]));
                        break;
                    // DotNet-native methods
                    case 4:
                        GetMimeTypeNative(args[1]);
                        break;
                    // DotNet-native methods (by urlmon.dll)
                    case 5:
                        // Ну тут блять как обычно
                        // System.AccessViolationException: Attempted to read or write protected memory. 
                        // This is often an indication that other memory is corrupt.
                        getMimeFromFile(args);
                        break;
                    // HeyRed.Mime
                    case 6:
                        string info = "MimeType: 1\nExtention: 2";
                        // MimeGuesser.MagicFilePath = args[1];
                        info = info.Replace("1", MimeGuesser.GuessMimeType(args[1])); //=> image/jpeg
                        // Get extension of file(overloaded method takes byte array or stream as arg.)
                        info = info.Replace("2", MimeGuesser.GuessExtension(args[1])); //=> jpeg
                        // Get mime type and extension of file(overloaded method takes byte array or stream as arg.)
                        RenderNativePannel(info);
                        // src: https://github.com/hey-red/Mime
                        break;
                    // DEFAULT
                    default:
                        OnPanic("По указанному ID не найдено ни 1 библиотеки :/");
                        break;
                }
            }
            else
            {
                OnPanic("Не удалось пропарсить ID библиотеки: идентификатор должен быть числом");
            }
        }
        #region Rendering results
        /// <summary>
        /// Render results using Spectre.Console native API:
        /// https://spectreconsole.net/
        /// </summary>
        private static void RenderNativePannel(string messageText)
        {
            var panel = new Panel(messageText ?? "NO INFO")
            {
                Header = new PanelHeader("Info about this file"),
                Border = BoxBorder.Rounded,
                Expand = true
            };
            AnsiConsole.Write(panel);
        }
        #endregion 

        #region Mime-nethods realization
        // DotNet-native methods
        private static void GetMimeTypeNative(string fullPath)
        {
            FileInfo fileInfo = new FileInfo(fullPath);
            if (fileInfo.Exists)
            {

                RenderNativePannel($"FileName: {fileInfo.Name}\n" +
                    $"Creation date: {fileInfo.CreationTime} ({fileInfo.CreationTimeUtc} UTC)\n" +
                    $"Size: {fileInfo.Length}\n" +
                    $"Is symlink: {((fileInfo.LinkTarget == null) ? "NO" : $"YES, to this file: {fileInfo.LinkTarget}")}\n" +
                    $"Extention: {fileInfo.Extension}\n" +
                    $"Stored in directory: {fileInfo.Directory} ({fileInfo.DirectoryName})\n" +
                    $"Arributes: {fileInfo.Attributes}");
            }
            else
            {
                OnPanic("Ты как сюда вообще попал...");
            }
        }
        // TwentyDevs.MimeTypeDetective
        private static void GetMimeTypeDetective(string fullPath)
        {
            var jsonFormatted = new JsonText(Newtonsoft.Json.JsonConvert.SerializeObject(MimeTypeDetection.GetMimeType(fullPath), Newtonsoft.Json.Formatting.Indented));
            // Using Spectre.Console.Json lib to render
            AnsiConsole.Write(
                new Panel(jsonFormatted)
                    .Header($"File {fullPath} (JSON-formatted)")
                    .Collapse()
                    .RoundedBorder()
                    .BorderColor(Color.Yellow));
        }
        // Mime-Detective
        /// <summary>
        /// Generates ContentInspectorBuilder thet used on a lib (2-d method)
        /// </summary>
        /// <param name="inspectorType">the inspector types: 1 - default, 2 - Condensed, 3 - Exhaustive</param>
        /// <returns></returns>
        private static IContentInspector GetContentInspectorBuilder(ushort inspectorType)
        {
            IContentInspector Inspector = new ContentInspectorBuilder().Build();

            switch (inspectorType)
            {
                // Default
                case 1:
                    Inspector = new ContentInspectorBuilder()
                    {
                        Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
                    }.Build();
                    break;
                // Condensed, need Mime-Detective.Definitions.Condensed to be installed 
                case 2:
                    Inspector = new ContentInspectorBuilder()
                    {
                        Definitions = new MimeDetective.Definitions.CondensedBuilder()
                        {
                            UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
                        }.Build()
                    }.Build();
                    break;
                // Exhaustive, need Mime-Detective.Definitions.Exhaustive to be installed  
                case 3:
                    Inspector = new ContentInspectorBuilder()
                    {
                        Definitions = new MimeDetective.Definitions.ExhaustiveBuilder()
                        {
                            UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
                        }.Build()
                    }.Build();
                    break;
                default:
                    OnPanic($"No builder with type {inspectorType} found. Mime-Detective, GetContentInspectorBuilder");
                    break;
            }
            return Inspector;
        }
        /// <summary>
        /// Используется для перебора ContentInspectorBuilder и вывода результатов в терминал 
        /// </summary>
        /// <param name="fullPath"></param>
        private static void ContentInspectorEnumeration(string fullPath)
        {
            if (File.ReadAllBytes(fullPath).Length == 0) OnPanic("The provided file is empty, so can't be inspected by this method");
            var inspector = new ContentInspectorBuilder { Definitions = DefaultDefinitions.All() }.Build();
            var result = inspector.InspectMemoryMapped(fullPath);
            foreach (var match in result.OrderByDescending(static m => m.Points))
            {
                if (match.Type != DefinitionMatchType.Complete)
                {
                    continue;
                }

                var fileType = match.Definition.File;
                if (string.IsNullOrEmpty(fileType.MimeType))
                {
                    continue;
                }

                Console.WriteLine($"  {fileType.MimeType} ({string.Join(", ", fileType.Extensions)})");
            }

            //for (ushort i = 1; i <= 3; i++)
            //{
            //var inspector = GetContentInspectorBuilder(i);
            //var result = inspector.Inspect(File.ReadAllBytes(fullPath));

            //var jsonFormatted = new JsonText(Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            // Using Spectre.Console.Json lib to render
            //AnsiConsole.Write(
            //    new Panel(jsonFormatted)
            //        .Header($"File {fullPath} (JSON-formatted, inspector type {i})")
            //        .Collapse()
            //        .RoundedBorder()
            //        .BorderColor(Color.Yellow));            
            //}
        }

        #region DotNet-native methods 

        enum PatternType { String, Hex }

        public static void getMimeFromFile(string[] args)
        {
            if (args.Length != 3) OnPanic($"To use this app MODE run:\n\t./{AppDomain.CurrentDomain.FriendlyName} [libID] [path_to_file] [path_to_magicfile]");
            string magicFile = string.Empty;
            string targetFile = string.Empty;
            if (!File.Exists(args[1])) { OnPanic($"Файл не существует: {args[1]}"); }
            else { targetFile = args[1]; }
            if (!File.Exists(args[2]))
            {
                OnPanic("Для использования этого метода требуетися наличие файла ./file.magic следующего формата (пример):\n" +
                    "# offset  type    pattern             description\n" +
                    "0          hex     89504E470D0A1A0A     PNG image data\n" +
                    "0          hex     FFD8FF               JPEG image data\n" +
                    "0          string  GIF87a              GIF image data (GIF87a)\n" +
                    "0          string  GIF89a              GIF image data (GIF89a)\n" +
                    "0          string  %PDF-               PDF document\n" +
                    "0          hex     504B0304             ZIP archive data\n" +
                    $"{String.Format("-", Console.BufferWidth)}");
            }
            else { magicFile = args[2]; }
            var detector = new Detector(magicFile);
            string result = detector.Detect(targetFile);
            RenderNativePannel($"Detected file MINME type:{string.Format(" ", Console.BufferWidth/2)}{result}");
        }
        #endregion

#endregion
    }
}
