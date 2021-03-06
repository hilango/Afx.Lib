﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Afx.HttpClient
{
    /// <summary>
    /// multipart/form-data 表单提交
    /// </summary>
    public class MultipartFormData : FormData
    {
        private Dictionary<string, string> paramDic;

        private Dictionary<string, string> fileDic;

        private const string NEW_LINE = "\r\n";

        private const string BOUNDARY = "----------------afx0httpclient0formdata";

        private const string BEGIN_BOUNDARY = "--" + BOUNDARY + NEW_LINE;

        private const string END_BOUNDARY= "--" + BOUNDARY + "--";

        private const string PARAM_CONTENT_DISPOSITION= "Content-Disposition: form-data; name=\"{0}\"" + NEW_LINE + NEW_LINE;

        private const string FILE_CONTENT_DISPOSITION = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\""
                + NEW_LINE + "Content-Type: application/octet-stream" + NEW_LINE + NEW_LINE;
        /// <summary>
        /// MultipartFormData
        /// </summary>
        public MultipartFormData()
        {
            this.ContentEncoding = Encoding.UTF8;
            this.paramDic = new Dictionary<string, string>();
            this.fileDic = new Dictionary<string, string>();

            this.ContentType = "multipart/form-data; charset=utf-8; boundary=" + BOUNDARY;
        }
        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddParam(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            this.paramDic[key] = value ?? "";
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetParam(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            if (this.paramDic.ContainsKey(key))
            {
                return this.paramDic[key];
            }

            return null;
        }
        /// <summary>
        /// 移除参数
        /// </summary>
        /// <param name="key"></param>
        public void RemoveParam(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            this.paramDic.Remove(key);
        }
        /// <summary>
        /// 添加上传文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fileName"></param>
        public void AddFile(string key, string fileName)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (!File.Exists(fileName)) throw new FileNotFoundException(fileName + " not found!", fileName);

            this.fileDic[key] = fileName;
        }
        /// <summary>
        /// 获取上传文件
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetFile(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            if (this.fileDic.ContainsKey(key))
            {
                return this.fileDic[key];
            }

            return null;
        }
        /// <summary>
        /// 移除上传文件
        /// </summary>
        /// <param name="key"></param>
        public void RemoveFile(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            this.fileDic.Remove(key);
        }
        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="stream"></param>
        public override void Serialize(Stream stream)
        {
            StringBuilder text = new StringBuilder();
            byte[] buffer = null;
            foreach (var kv in this.paramDic)
            {
                text.Append(BEGIN_BOUNDARY);
                text.AppendFormat(PARAM_CONTENT_DISPOSITION, kv.Key);
                text.Append(kv.Value);
                text.Append(NEW_LINE);
            }

            if (text.Length > 0)
            {
                buffer = this.ContentEncoding.GetBytes(text.ToString());
                stream.Write(buffer, 0, buffer.Length);
            }

            text = new StringBuilder();
            foreach (var kv in this.fileDic)
            {
                //text.Clear();
                text.Append(BEGIN_BOUNDARY);
                text.AppendFormat(FILE_CONTENT_DISPOSITION, kv.Key, kv.Value);

                buffer = this.ContentEncoding.GetBytes(text.ToString());
                stream.Write(buffer, 0, buffer.Length);

                using (var fs = File.Open(kv.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    buffer = new byte[4 * 1024];
                    int count = 0;
                    while ((count = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, count);
                    }
                }

                buffer = this.ContentEncoding.GetBytes(NEW_LINE);
                stream.Write(buffer, 0, buffer.Length);
            }

            if (paramDic.Count > 0 || fileDic.Count > 0)
            {
                buffer = this.ContentEncoding.GetBytes(END_BOUNDARY);
                stream.Write(buffer, 0, buffer.Length);
            }

           // text.Clear();
            text = null;
        }
        /// <summary>
        /// GetLength
        /// </summary>
        /// <returns></returns>
        public override long GetLength()
        {
            long length = 0;
            StringBuilder text = new StringBuilder();
            foreach (var kv in this.paramDic)
            {
                text.Append(BEGIN_BOUNDARY);
                text.AppendFormat(PARAM_CONTENT_DISPOSITION, kv.Key);
                text.Append(kv.Value);
                text.Append(NEW_LINE);
            }

            foreach (var kv in this.fileDic)
            {
                text.Append(BEGIN_BOUNDARY);
                text.AppendFormat(FILE_CONTENT_DISPOSITION, kv.Key, kv.Value);

                var fileInfo = new FileInfo(kv.Value);
                length += fileInfo.Length;

                text.Append(NEW_LINE);
            }

            if (paramDic.Count > 0 || fileDic.Count > 0)
            {
                text.Append(END_BOUNDARY);
            }

            if (text.Length > 0)
            {
                length += this.ContentEncoding.GetByteCount(text.ToString());
            }

           // text.Clear();
            text = null;

            return length;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            if (this.paramDic != null) this.paramDic.Clear();
            if (this.fileDic != null) this.fileDic.Clear();
            this.paramDic = null;
            this.fileDic = null;
        }
    }
}
