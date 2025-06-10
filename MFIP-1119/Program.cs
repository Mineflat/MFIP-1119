using Spectre.Console;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MimeDetective;
using Spectre.Console.Json;
using TwentyDevs.MimeTypeDetective;
using MimeSharp;
using System.IO;
using HeyRed.Mime;

namespace MFIP_1119
{
    internal class Program
    {
        private static void ShowHelpMessage()
        {
            Console.WriteLine($"To use this app run:\n\t./{AppDomain.CurrentDomain.FriendlyName} [libID] [path_to_file]");
            Console.WriteLine("List fo LibID:");
            Console.WriteLine("\t [1] - Mime-Detective");
            Console.WriteLine("\t [2] - TwentyDevs.MimeTypeDetective");
            Console.WriteLine("\t [3] - MimeSharp");
            Console.WriteLine("\t [4] - TikaOnDotNet");
            Console.WriteLine("\t [5] - DotNet-native methods (by urlmon.dll) - WILL NOT WORK FOR NOW" +
                "\t[6] - HeyRed.Mime");
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
            if (args.Length != 2)
            {
                OnPanic($"Передано неверное число аргументов: {args.Length}");
            }
            // Парсинг ID библиотеки, которая будет испльзоваться
            if (ushort.TryParse(args[0], out ushort libID))
            {
                // Проверка существования файла
                if (!File.Exists(args[1]))
                {
                    OnPanic($"Указанный файл не существует или не хватает прав для его чтения: {args[2]}");
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
                        OnPanic("Этот метод пока не работает");
                        RenderNativePannel($"File mime type: {getMimeFromFile(args[1])}");
                        break;
                    // HeyRed.Mime
                    case 6:
                        string info = "MimeType: 1\nExtention: 2";
                        //MimeGuesser.MagicFilePath = args[1];
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
            for (ushort i = 1; i <= 3; i++)
            {
                var inspector = GetContentInspectorBuilder(i);
                var result = inspector.Inspect(fullPath);
                var jsonFormatted = new JsonText(Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
                // Using Spectre.Console.Json lib to render
                AnsiConsole.Write(
                    new Panel(jsonFormatted)
                        .Header($"File {fullPath} (JSON-formatted, inspector type {i})")
                        .Collapse()
                        .RoundedBorder()
                        .BorderColor(Color.Yellow));
            }
        }

        // DotNet-native methods (by urlmon.dll)
        [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
        private extern static System.UInt32 FindMimeFromData(
            System.UInt32 pBC,
            [MarshalAs(UnmanagedType.LPStr)] System.String pwzUrl,
            [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
            System.UInt32 cbSize,
            [MarshalAs(UnmanagedType.LPStr)] System.String pwzMimeProposed,
            System.UInt32 dwMimeFlags,
            out System.UInt32 ppwzMimeOut,
            System.UInt32 dwReserverd
        );

        public static string getMimeFromFile(string fullPath)
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(fullPath + " not found");

            byte[] buffer = new byte[256];
            using (FileStream fs = new FileStream(fullPath, FileMode.Open))
            {
                if (fs.Length >= 256)
                    fs.Read(buffer, 0, 256);
                else
                    fs.Read(buffer, 0, (int)fs.Length);
            }
            try
            {
                System.UInt32 mimetype;
#pragma warning disable CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
                FindMimeFromData(0, null, buffer, 256, null, 0, out mimetype, 0);
                System.IntPtr mimeTypePtr = new IntPtr(mimetype);
                string mime = Marshal.PtrToStringUni(mimeTypePtr);
#pragma warning restore CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
                Marshal.FreeCoTaskMem(mimeTypePtr);
                return mime;
            }
            catch (Exception e)
            {
                return "unknown/unknown";
            }
        }
        #endregion
    }
}
