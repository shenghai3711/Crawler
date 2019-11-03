using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HZ.Crawler.Common
{
    public class FileHelper
    {

        #region CreateIfNotExists(创建文件，如果文件不存在)

        /// <summary>
        /// 创建文件，如果文件不存在
        /// </summary>
        /// <param name="fileName">文件名，绝对路径</param>
        public static void CreateIfNotExists(string fileName)
        {
            FileInfo file = new FileInfo(fileName);
            if (!Directory.Exists(file.DirectoryName))
                Directory.CreateDirectory(file.DirectoryName);
            if (!File.Exists(fileName))
                File.Create(fileName).Close();
        }

        #endregion

        #region Delete(删除文件)

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePaths">文件集合的绝对路径</param>
        public static void Delete(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Delete(filePath);
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        public static void Delete(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }
            if (File.Exists(filePath))
            {
                return;
            }
            File.Delete(filePath);
        }

        #endregion

        #region KillFile(强力粉碎文件)
        /// <summary>
        /// 强力粉碎文件，如果文件被打开，很难粉碎
        /// </summary>
        /// <param name="fileName">文件全路径</param>
        /// <param name="deleteCount">删除次数</param>
        /// <param name="randomData">随机数据填充文件，默认true</param>
        /// <param name="blanks">空白填充文件，默认false</param>
        /// <returns>true:粉碎成功,false:粉碎失败</returns>        
        public static bool KillFile(string fileName, int deleteCount, bool randomData = true, bool blanks = false)
        {
            const int bufferLength = 1024000;
            bool ret = true;
            try
            {
                using (
                    FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    FileInfo file = new FileInfo(fileName);
                    long count = file.Length;
                    long offset = 0;
                    var rowDataBuffer = new byte[bufferLength];
                    while (count >= 0)
                    {
                        int iNumOfDataRead = stream.Read(rowDataBuffer, 0, bufferLength);
                        if (iNumOfDataRead == 0)
                        {
                            break;
                        }
                        if (randomData)
                        {
                            Random randomByte = new Random();
                            randomByte.NextBytes(rowDataBuffer);
                        }
                        else if (blanks)
                        {
                            for (int i = 0; i < iNumOfDataRead; i++)
                            {
                                rowDataBuffer[i] = Convert.ToByte(Convert.ToChar(deleteCount));
                            }
                        }
                        //写新内容到文件
                        for (int i = 0; i < deleteCount; i++)
                        {
                            stream.Seek(offset, SeekOrigin.Begin);
                            stream.Write(rowDataBuffer, 0, iNumOfDataRead);
                            ;
                        }
                        offset += iNumOfDataRead;
                        count -= iNumOfDataRead;
                    }
                }
                //每一个文件名字符代替随机数从0到9
                string newName = "";
                do
                {
                    Random random = new Random();
                    string cleanName = Path.GetFileName(fileName);
                    string dirName = Path.GetDirectoryName(fileName);
                    int iMoreRandomLetters = random.Next(9);
                    //为了更安全，不要只使用原文件名的大小，添加一些随机字母
                    for (int i = 0; i < cleanName.Length + iMoreRandomLetters; i++)
                    {
                        newName += random.Next(9).ToString();
                    }
                    newName = dirName + "\\" + newName;
                } while (File.Exists(newName));
                //重命名文件的新随机的名字
                File.Move(fileName, newName);
                File.Delete(newName);
            }
            catch
            {
                //可能其他原因删除失败，使用我们自己的方法强制删除
                try
                {
                    string filename = fileName;//要检查被哪个进程占用的文件
                    Process tool = new Process()
                    {
                        StartInfo =
                        {
                            FileName = "handle.exe",
                            Arguments = filename + " /accepteula",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        }
                    };
                    tool.Start();
                    tool.WaitForExit();
                    string outputTool = tool.StandardOutput.ReadToEnd();
                    string matchPattern = @"(?<=\s+pid:\s+)\b(\d+)\b(?=\s+)";
                    foreach (Match match in Regex.Matches(outputTool, matchPattern))
                    {
                        //结束掉所有正在使用这个文件的程序
                        Process.GetProcessById(int.Parse(match.Value)).Kill();
                    }
                    File.Delete(filename);
                }
                catch
                {

                    ret = false;
                }
            }
            return ret;
        }
        #endregion

        #region SetAttribute(设置文件属性)

        /// <summary>
        /// 设置文件属性
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="attribute">文件属性</param>
        /// <param name="isSet">是否为设置属性,true:设置,false:取消</param>
        public static void SetAttribute(string fileName, FileAttributes attribute, bool isSet)
        {
            FileInfo fi = new FileInfo(fileName);
            if (!fi.Exists)
            {
                throw new FileNotFoundException("要设置属性的文件不存在。", fileName);
            }
            if (isSet)
            {
                fi.Attributes = fi.Attributes | attribute;
            }
            else
            {
                fi.Attributes = fi.Attributes & ~attribute;
            }
        }

        #endregion

        #region GetVersion(获取文件版本号)

        /// <summary>
        /// 获取文件版本号
        /// </summary>
        /// <param name="fileName">完整文件名</param>
        /// <returns></returns>
        public static string GetVersion(string fileName)
        {
            if (File.Exists(fileName))
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                return fvi.FileVersion;
            }
            return null;
        }

        #endregion

        #region GetFileMd5(获取文件的MD5值)

        /// <summary>
        /// 获取文件的MD5值
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static string GetFileMd5(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            md5.Initialize();

            long offset = 0;
            while (offset < fs.Length)
            {
                long readSize = bufferSize;
                if (offset + readSize > fs.Length)
                {
                    readSize = fs.Length - offset;
                }
                fs.Read(buffer, 0, (int)readSize);
                if (offset + readSize < fs.Length)
                {
                    md5.TransformBlock(buffer, 0, (int)readSize, buffer, 0);
                }
                else
                {
                    md5.TransformFinalBlock(buffer, 0, (int)readSize);
                }
                offset += bufferSize;
            }
            fs.Close();
            byte[] result = md5.Hash;
            md5.Clear();
            StringBuilder sb = new StringBuilder();
            foreach (var b in result)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        #endregion

        #region GetEncoding(获取文件编码)
        /// <summary>
        /// 获取文件编码
        /// </summary>
        /// <param name="filePath">文件绝对路径</param>
        /// <returns></returns>
        public static Encoding GetEncoding(string filePath)
        {
            return GetEncoding(filePath, Encoding.Default);
        }
        /// <summary>
        /// 获取文件编码
        /// </summary>
        /// <param name="filePath">文件绝对路径</param>
        /// <param name="defaultEncoding">默认编码</param>
        /// <returns></returns>
        public static Encoding GetEncoding(string filePath, Encoding defaultEncoding)
        {
            Encoding targetEncoding = defaultEncoding;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4))
            {
                if (fs != null && fs.Length >= 2)
                {
                    long pos = fs.Position;
                    fs.Position = 0;
                    int[] buffer = new int[4];
                    buffer[0] = fs.ReadByte();
                    buffer[1] = fs.ReadByte();
                    buffer[2] = fs.ReadByte();
                    buffer[3] = fs.ReadByte();
                    fs.Position = pos;

                    if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                    {
                        targetEncoding = Encoding.BigEndianUnicode;
                    }
                    if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                    {
                        targetEncoding = Encoding.Unicode;
                    }
                    if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                    {
                        targetEncoding = Encoding.UTF8;
                    }
                }
            }
            return targetEncoding;
        }
        #endregion

        #region GetAllFiles(获取目录中全部文件列表)
        /// <summary>
        /// 获取目录中全部文件列表，包括子目录
        /// </summary>
        /// <param name="directoryPath">目录绝对路径</param>
        /// <returns></returns>
        public static List<string> GetAllFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToList();
        }
        #endregion

        #region GetContentType(根据扩展名获取文件内容类型)
        /// <summary>
        /// 根据扩展名获取文件内容类型
        /// </summary>
        /// <param name="ext">扩展名</param>
        /// <returns></returns>
        public static string GetContentType(string ext)
        {
            string contentType = "";
            var dict = FileExtensionDict;
            ext = ext.ToLower();
            if (!ext.StartsWith("."))
            {
                ext = "." + ext;
            }
            dict.TryGetValue(ext, out contentType);
            return contentType;
        }

        #endregion

        #region Read(读取文件到字符串)

        /// <summary>
        /// 读取文件到字符串
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns></returns>
        public static string Read(string filePath)
        {
            return Read(filePath, DefaultEncoding);
        }

        /// <summary>
        /// 读取文件到字符串
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="encoding">字符编码</param>
        /// <returns></returns>
        public static string Read(string filePath, Encoding encoding)
        {
            if (!File.Exists(filePath))
                return string.Empty;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                StreamReader reader = new StreamReader(fs, encoding);
                StringBuilder sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    sb.AppendLine(reader.ReadLine());
                }
                return sb.ToString();
            }
        }

        #endregion

        #region ReadToBytes(将文件读取到字节流中)
        /// <summary>
        /// 将文件读取到字节流中
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns></returns>
        public static byte[] ReadToBytes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            FileInfo fileInfo = new FileInfo(filePath);
            int fileSize = (int)fileInfo.Length;
            using (BinaryReader reader = new BinaryReader(fileInfo.Open(FileMode.Open)))
            {
                return reader.ReadBytes(fileSize);
            }
        }
        #endregion

        #region Write(将字节流写入文件)

        /// <summary>
        /// 将字符串写入文件，文件不存在则创建
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="content">数据</param>
        public static void Write(string filePath, string content)
        {
            Write(filePath, ToBytes(content));
        }

        /// <summary>
        /// 将字符串写入文件，文件不存在则创建
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="bytes">数据</param>
        public static void Write(string filePath, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }
            if (bytes == null)
            {
                return;
            }
            File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region ToString(转换成字符串)
        /// <summary>
        /// 流转换成字符串
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static string ToString(Stream data)
        {
            return ToString(data, DefaultEncoding);
        }

        /// <summary>
        /// 流转换成字符串
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="encoding">字符编码</param>
        /// <returns></returns>
        public static string ToString(Stream data, Encoding encoding)
        {
            if (data == null)
            {
                return string.Empty;
            }
            string result;
            using (var reader = new StreamReader(data, encoding))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        /// <summary>
        /// 字节数组转换成字符串
        /// </summary>
        /// <param name="data">数据,默认字符编码utf-8</param>
        /// <returns></returns>
        public static string ToString(byte[] data)
        {
            return ToString(data, DefaultEncoding);
        }

        /// <summary>
        /// 字节数组转换成字符串
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="encoding">字符编码</param>
        /// <returns></returns>
        public static string ToString(byte[] data, Encoding encoding)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }
            return encoding.GetString(data);
        }
        #endregion

        #region ToStream(转换成流)
        /// <summary>
        /// 字符串转换成流
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static Stream ToStream(string data)
        {
            return ToStream(data, DefaultEncoding);
        }

        /// <summary>
        /// 字符串转换成流
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="encoding">字符编码</param>
        /// <returns></returns>
        public static Stream ToStream(string data, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Stream.Null;
            }
            return new MemoryStream(ToBytes(data, encoding));
        }

        #endregion

        #region ToBytes(转换成字节数组)
        /// <summary>
        /// 将字符串转换成字节数组
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static byte[] ToBytes(string data)
        {
            return ToBytes(data, DefaultEncoding);
        }

        /// <summary>
        /// 字符串转换成字节数组
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="encoding">字符编码</param>
        /// <returns></returns>
        public static byte[] ToBytes(string data, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return new byte[] { };
            }
            return encoding.GetBytes(data);
        }

        /// <summary>
        /// 流转换成字节流
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns></returns>
        public static byte[] ToBytes(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        #endregion

        #region ToInt(转换成整数)
        /// <summary>
        /// 字节数组转换成整数
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static int ToInt(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                return 0;
            }
            var buffer = new byte[4];
            Buffer.BlockCopy(data, 0, buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }
        #endregion

        #region JoinPath(连接基路径和子路径)

        /// <summary>
        /// 连接基路径和子路径，比如把 c: 与 test.doc 连接成 c:\test.doc
        /// </summary>
        /// <param name="basePath">基路径，范例：c:</param>
        /// <param name="subPath">子路径，可以是文件名，范例：test.doc</param>
        /// <returns></returns>
        public static string JoinPath(string basePath, string subPath)
        {
            basePath = basePath.TrimEnd('/').TrimEnd('\\');
            subPath = subPath.TrimStart('/').TrimStart('\\');
            string path = basePath + "\\" + subPath;
            return path.Replace("/", "\\").ToLower();
        }

        #endregion

        #region FileExtensionDict(文件扩展类型字典)

        /// <summary>
        /// 文件扩展类型字典
        /// </summary>
        public static Dictionary<string, string> FileExtensionDict = new Dictionary<string, string>()
        {
            {".*", "application/octet-stream"},
            {".tif", "image/tiff"},
            {".001", "application/x-001"},
            {".301", "application/x-301"},
            {".323", "text/h323"},
            {".906", "application/x-906"},
            {".907", "drawing/907"},
            {".a11", "application/x-a11"},
            {".acp", "audio/x-mei-aac"},
            {".ai", "application/postscript"},
            {".aif", "audio/aiff"},
            {".aifc", "audio/aiff"},
            {".aiff", "audio/aiff"},
            {".anv", "application/x-anv"},
            {".asa", "text/asa"},
            {".asf", "video/x-ms-asf"},
            {".asp", "text/asp"},
            {".asx", "video/x-ms-asf"},
            {".au", "audio/basic"},
            {".avi", "video/avi"},
            {".awf", "application/vnd.adobe.workflow"},
            {".biz", "text/xml"},
            {".bmp", "application/x-bmp"},
            {".bot", "application/x-bot"},
            {".c4t", "application/x-c4t"},
            {".c90", "application/x-c90"},
            {".cal", "application/x-cals"},
            {".cat", "application/vnd.ms-pki.seccat"},
            {".cdf", "application/x-netcdf"},
            {".cdr", "application/x-cdr"},
            {".cel", "application/x-cel"},
            {".cer", "application/x-x509-ca-cert"},
            {".cg4", "application/x-g4"},
            {".cgm", "application/x-cgm"},
            {".cit", "application/x-cit"},
            {".class", "java/*"},
            {".cml", "text/xml"},
            {".cmp", "application/x-cmp"},
            {".cmx", "application/x-cmx"},
            {".cot", "application/x-cot"},
            {".crl", "application/pkix-crl"},
            {".crt", "application/x-x509-ca-cert"},
            {".csi", "application/x-csi"},
            {".css", "text/css"},
            {".cut", "application/x-cut"},
            {".dbf", "application/x-dbf"},
            {".dbm", "application/x-dbm"},
            {".dbx", "application/x-dbx"},
            {".dcd", "text/xml"},
            {".dcx", "application/x-dcx"},
            {".der", "application/x-x509-ca-cert"},
            {".dgn", "application/x-dgn"},
            {".dib", "application/x-dib"},
            {".dll", "application/x-msdownload"},
            {".doc", "application/msword"},
            {".dot", "application/msword"},
            {".drw", "application/x-drw"},
            {".dtd", "text/xml"},
            {".dwf", "Model/vnd.dwf"},
            {".dwg", "application/x-dwg"},
            {".dxb", "application/x-dxb"},
            {".dxf", "application/x-dxf"},
            {".edn", "application/vnd.adobe.edn"},
            {".emf", "application/x-emf"},
            {".eml", "message/rfc822"},
            {".ent", "text/xml"},
            {".epi", "application/x-epi"},
            {".eps", "application/x-ps"},
            {".etd", "application/x-ebx"},
            {".exe", "application/x-msdownload"},
            {".fax", "image/fax"},
            {".fdf", "application/vnd.fdf"},
            {".fif", "application/fractals"},
            {".fo", "text/xml"},
            {".frm", "application/x-frm"},
            {".g4", "application/x-g4"},
            {".gbr", "application/x-gbr"},
            {".", "application/x-"},
            {".gif", "image/gif"},
            {".gl2", "application/x-gl2"},
            {".gp4", "application/x-gp4"},
            {".hgl", "application/x-hgl"},
            {".hmr", "application/x-hmr"},
            {".hpg", "application/x-hpgl"},
            {".hpl", "application/x-hpl"},
            {".hqx", "application/mac-binhex40"},
            {".hrf", "application/x-hrf"},
            {".hta", "application/hta"},
            {".htc", "text/x-component"},
            {".htm", "text/html"},
            {".html", "text/html"},
            {".htt", "text/webviewhtml"},
            {".htx", "text/html"},
            {".icb", "application/x-icb"},
            {".ico", "image/x-icon"},
            {".iff", "application/x-iff"},
            {".ig4", "application/x-g4"},
            {".igs", "application/x-igs"},
            {".iii", "application/x-iphone"},
            {".img", "application/x-img"},
            {".ins", "application/x-internet-signup"},
            {".isp", "application/x-internet-signup"},
            {".IVF", "video/x-ivf"},
            {".java", "java/*"},
            {".jfif", "image/jpeg"},
            {".jpe", "application/x-jpe"},
            {".jpeg", "image/jpeg"},
            {".jpg", "image/jpeg"},
            {".js", "application/x-javascript"},
            {".jsp", "text/html"},
            {".la1", "audio/x-liquid-file"},
            {".lar", "application/x-laplayer-reg"},
            {".latex", "application/x-latex"},
            {".lavs", "audio/x-liquid-secure"},
            {".lbm", "application/x-lbm"},
            {".lmsff", "audio/x-la-lms"},
            {".ls", "application/x-javascript"},
            {".ltr", "application/x-ltr"},
            {".m1v", "video/x-mpeg"},
            {".m2v", "video/x-mpeg"},
            {".m3u", "audio/mpegurl"},
            {".m4e", "video/mpeg4"},
            {".mac", "application/x-mac"},
            {".man", "application/x-troff-man"},
            {".math", "text/xml"},
            {".mdb", "application/msaccess"},
            {".mfp", "application/x-shockwave-flash"},
            {".mht", "message/rfc822"},
            {".mhtml", "message/rfc822"},
            {".mi", "application/x-mi"},
            {".mid", "audio/mid"},
            {".midi", "audio/mid"},
            {".mil", "application/x-mil"},
            {".mml", "text/xml"},
            {".mnd", "audio/x-musicnet-download"},
            {".mns", "audio/x-musicnet-stream"},
            {".mocha", "application/x-javascript"},
            {".movie", "video/x-sgi-movie"},
            {".mp1", "audio/mp1"},
            {".mp2", "audio/mp2"},
            {".mp2v", "video/mpeg"},
            {".mp3", "audio/mp3"},
            {".mp4", "video/mpeg4"},
            {".mpa", "video/x-mpg"},
            {".mpd", "application/vnd.ms-project"},
            {".mpe", "video/x-mpeg"},
            {".mpeg", "video/mpg"},
            {".mpg", "video/mpg"},
            {".mpga", "audio/rn-mpeg"},
            {".mpp", "application/vnd.ms-project"},
            {".mps", "video/x-mpeg"},
            {".mpt", "application/vnd.ms-project"},
            {".mpv", "video/mpg"},
            {".mpv2", "video/mpeg"},
            {".mpw", "application/vnd.ms-project"},
            {".mpx", "application/vnd.ms-project"},
            {".mtx", "text/xml"},
            {".mxp", "application/x-mmxp"},
            {".net", "image/pnetvue"},
            {".nrf", "application/x-nrf"},
            {".nws", "message/rfc822"},
            {".odc", "text/x-ms-odc"},
            {".out", "application/x-out"},
            {".p10", "application/pkcs10"},
            {".p12", "application/x-pkcs12"},
            {".p7b", "application/x-pkcs7-certificates"},
            {".p7c", "application/pkcs7-mime"},
            {".p7m", "application/pkcs7-mime"},
            {".p7r", "application/x-pkcs7-certreqresp"},
            {".p7s", "application/pkcs7-signature"},
            {".pc5", "application/x-pc5"},
            {".pci", "application/x-pci"},
            {".pcl", "application/x-pcl"},
            {".pcx", "application/x-pcx"},
            {".pdf", "application/pdf"},
            {".pdx", "application/vnd.adobe.pdx"},
            {".pfx", "application/x-pkcs12"},
            {".pgl", "application/x-pgl"},
            {".pic", "application/x-pic"},
            {".pko", "application/vnd.ms-pki.pko"},
            {".pl", "application/x-perl"},
            {".plg", "text/html"},
            {".pls", "audio/scpls"},
            {".plt", "application/x-plt"},
            {".png", "image/png"},
            {".pot", "application/vnd.ms-powerpoint"},
            {".ppa", "application/vnd.ms-powerpoint"},
            {".ppm", "application/x-ppm"},
            {".pps", "application/vnd.ms-powerpoint"},
            {".ppt", "application/vnd.ms-powerpoint"},
            {".pr", "application/x-pr"},
            {".prf", "application/pics-rules"},
            {".prn", "application/x-prn"},
            {".prt", "application/x-prt"},
            {".ps", "application/x-ps"},
            {".ptn", "application/x-ptn"},
            {".pwz", "application/vnd.ms-powerpoint"},
            {".r3t", "text/vnd.rn-realtext3d"},
            {".ra", "audio/vnd.rn-realaudio"},
            {".ram", "audio/x-pn-realaudio"},
            {".ras", "application/x-ras"},
            {".rat", "application/rat-file"},
            {".rdf", "text/xml"},
            {".rec", "application/vnd.rn-recording"},
            {".red", "application/x-red"},
            {".rgb", "application/x-rgb"},
            {".rjs", "application/vnd.rn-realsystem-rjs"},
            {".rjt", "application/vnd.rn-realsystem-rjt"},
            {".rlc", "application/x-rlc"},
            {".rle", "application/x-rle"},
            {".rm", "application/vnd.rn-realmedia"},
            {".rmf", "application/vnd.adobe.rmf"},
            {".rmi", "audio/mid"},
            {".rmj", "application/vnd.rn-realsystem-rmj"},
            {".rmm", "audio/x-pn-realaudio"},
            {".rmp", "application/vnd.rn-rn_music_package"},
            {".rms", "application/vnd.rn-realmedia-secure"},
            {".rmvb", "application/vnd.rn-realmedia-vbr"},
            {".rmx", "application/vnd.rn-realsystem-rmx"},
            {".rnx", "application/vnd.rn-realplayer"},
            {".rp", "image/vnd.rn-realpix"},
            {".rpm", "audio/x-pn-realaudio-plugin"},
            {".rsml", "application/vnd.rn-rsml"},
            {".rt", "text/vnd.rn-realtext"},
            {".rtf", "application/msword"},
            {".rv", "video/vnd.rn-realvideo"},
            {".sam", "application/x-sam"},
            {".sat", "application/x-sat"},
            {".sdp", "application/sdp"},
            {".sdw", "application/x-sdw"},
            {".sit", "application/x-stuffit"},
            {".slb", "application/x-slb"},
            {".sld", "application/x-sld"},
            {".slk", "drawing/x-slk"},
            {".smi", "application/smil"},
            {".smil", "application/smil"},
            {".smk", "application/x-smk"},
            {".snd", "audio/basic"},
            {".sol", "text/plain"},
            {".sor", "text/plain"},
            {".spc", "application/x-pkcs7-certificates"},
            {".spl", "application/futuresplash"},
            {".spp", "text/xml"},
            {".ssm", "application/streamingmedia"},
            {".sst", "application/vnd.ms-pki.certstore"},
            {".stl", "application/vnd.ms-pki.stl"},
            {".stm", "text/html"},
            {".sty", "application/x-sty"},
            {".svg", "text/xml"},
            {".swf", "application/x-shockwave-flash"},
            {".tdf", "application/x-tdf"},
            {".tg4", "application/x-tg4"},
            {".tga", "application/x-tga"},
            {".tiff", "image/tiff"},
            {".tld", "text/xml"},
            {".top", "drawing/x-top"},
            {".torrent", "application/x-bittorrent"},
            {".tsd", "text/xml"},
            {".txt", "text/plain"},
            {".uin", "application/x-icq"},
            {".uls", "text/iuls"},
            {".vcf", "text/x-vcard"},
            {".vda", "application/x-vda"},
            {".vdx", "application/vnd.visio"},
            {".vml", "text/xml"},
            {".vpg", "application/x-vpeg005"},
            {".vsd", "application/vnd.visio"},
            {".vss", "application/vnd.visio"},
            {".vst", "application/vnd.visio"},
            {".vsw", "application/vnd.visio"},
            {".vsx", "application/vnd.visio"},
            {".vtx", "application/vnd.visio"},
            {".vxml", "text/xml"},
            {".wav", "audio/wav"},
            {".wax", "audio/x-ms-wax"},
            {".wb1", "application/x-wb1"},
            {".wb2", "application/x-wb2"},
            {".wb3", "application/x-wb3"},
            {".wbmp", "image/vnd.wap.wbmp"},
            {".wiz", "application/msword"},
            {".wk3", "application/x-wk3"},
            {".wk4", "application/x-wk4"},
            {".wkq", "application/x-wkq"},
            {".wks", "application/x-wks"},
            {".wm", "video/x-ms-wm"},
            {".wma", "audio/x-ms-wma"},
            {".wmd", "application/x-ms-wmd"},
            {".wmf", "application/x-wmf"},
            {".wml", "text/vnd.wap.wml"},
            {".wmv", "video/x-ms-wmv"},
            {".wmx", "video/x-ms-wmx"},
            {".wmz", "application/x-ms-wmz"},
            {".wp6", "application/x-wp6"},
            {".wpd", "application/x-wpd"},
            {".wpg", "application/x-wpg"},
            {".wpl", "application/vnd.ms-wpl"},
            {".wq1", "application/x-wq1"},
            {".wr1", "application/x-wr1"},
            {".wri", "application/x-wri"},
            {".wrk", "application/x-wrk"},
            {".ws", "application/x-ws"},
            {".ws2", "application/x-ws"},
            {".wsc", "text/scriptlet"},
            {".wsdl", "text/xml"},
            {".wvx", "video/x-ms-wvx"},
            {".xdp", "application/vnd.adobe.xdp"},
            {".xdr", "text/xml"},
            {".xfd", "application/vnd.adobe.xfd"},
            {".xfdf", "application/vnd.adobe.xfdf"},
            {".xhtml", "text/html"},
            {".xls", "application/vnd.ms-excel"},
            {".xlw", "application/x-xlw"},
            {".xml", "text/xml"},
            {".xpl", "audio/scpls"},
            {".xq", "text/xml"},
            {".xql", "text/xml"},
            {".xquery", "text/xml"},
            {".xsd", "text/xml"},
            {".xsl", "text/xml"},
            {".xslt", "text/xml"},
            {".xwd", "application/x-xwd"},
            {".x_b", "application/x-x_b"},
            {".sis", "application/vnd.symbian.install"},
            {".sisx", "application/vnd.symbian.install"},
            {".x_t", "application/x-x_t"},
            {".ipa", "application/vnd.iphone"},
            {".apk", "application/vnd.android.package-archive"},
            {".xap", "application/x-silverlight-app"}
        };

        #endregion


        /// <summary>
        /// 默认编码，值为UTF-8
        /// </summary>
        public static Encoding DefaultEncoding = Encoding.UTF8;
    }
}
