﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Entity.Schema
{
    /// <summary>
    /// 获取model信息
    /// </summary>
    public abstract class ModelSchema : IModelSchema
    {
        /// <summary>
        /// 获取 model .cs 文件源代码
        /// </summary>
        /// <param name="modelName">model名称</param>
        /// <param name="columns">表列信息</param>
        /// <param name="namespace">model 命名空间</param>
        /// <returns>model .cs 文件源代码</returns>
        public virtual string GetModelCode(string modelName, List<ColumnInfoModel> columns, string @namespace)
        {
            if (string.IsNullOrEmpty(modelName)) throw new ArgumentNullException("modelName");
            if(columns == null) throw new ArgumentNullException("columns");
            if (string.IsNullOrEmpty(@namespace)) throw new ArgumentNullException("namespace");
            StringBuilder propertysString = new StringBuilder();
            if (columns != null && columns.Count > 0)
            {
                foreach (var column in columns)
                {
                    propertysString.Append(this.GetAttributeCode(column));
                    propertysString.Append(this.GetPropertyCode(column));
                    propertysString.Append("\r\n\r\n");
                }
                propertysString.Remove(propertysString.Length - 4, 4);
            }

            return string.Format(ModelFormat, @namespace, modelName, this.GetModelName(modelName), propertysString.ToString());
        }

        /// <summary>
        /// 获取model名称
        /// </summary>
        /// <param name="table">表名</param>
        /// <returns>model名称</returns>
        public virtual string GetModelName(string table)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            string name = table;
            if (name.Contains('_'))
            {
                var arr = name.Split('_');
                name = "";
                foreach (var s in arr)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        name = name + s.Substring(0, 1).ToUpper();
                        if (s.Length > 1) name = name + s.Substring(1, s.Length - 1);
                    }
                }
            }

            name.Replace('.', '_');
            char c = name[0];
            if ('0' <= c && c <= '9' || c == '#' || c == '$')
            {
                name = "_" + name;
            }

            return name;
        }

        /// <summary>
        /// 获取model属性标记源代码
        /// </summary>
        /// <param name="column">列信息</param>
        /// <returns>model属性标记源代码</returns>
        public virtual string GetAttributeCode(ColumnInfoModel column)
        {
            if (column == null) throw new ArgumentNullException("column");
            StringBuilder attsString = new StringBuilder();
            string attrFormat = this.GetPropertyAttribute();
            if (column.IsKey)
            {
                attsString.AppendLine(string.Format(attrFormat, "Key"));
            }

            if (!column.IsKey && !column.IsNullable)
            {
                attsString.AppendLine(string.Format(attrFormat, "Required"));
            }

            string s = null;
            string propertyType = this.GetPropertyType(column);
            if (string.Equals("string", propertyType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("byte[]", propertyType, StringComparison.OrdinalIgnoreCase))
            {
                s = string.Format("MaxLength({0})", column.MaxLength);
                attsString.AppendLine(string.Format(attrFormat, s));
            }
            else if (string.Equals("decimal", propertyType, StringComparison.OrdinalIgnoreCase))
            {
                s = string.Format("Decimal({0}, {1})", column.MaxLength, column.MinLength);
                attsString.AppendLine(string.Format(attrFormat, s));
            }

            s = string.Format("Column(\"{0}\")", column.Name); //string.Format("Column(\"{0}\",Order={1})", column.Name, column.Order);
            attsString.AppendLine(string.Format(attrFormat, s));
            
            
            if (!column.IsKey && !string.IsNullOrEmpty(column.IndexName))
            {
                s = string.Format("Index(\"{0}\", IsUnique = {1})", column.IndexName, column.IsUnique ? "true" : "false");
                attsString.AppendLine(string.Format(attrFormat, s));
            }

            if (string.Equals("int", propertyType, StringComparison.OrdinalIgnoreCase)
                || string.Equals("long", propertyType, StringComparison.OrdinalIgnoreCase))
            {
                if (column.IsAutoIncrement)
                {
                    attsString.AppendLine(string.Format(attrFormat, "DatabaseGenerated(DatabaseGeneratedOption.Identity)"));
                }
                else if (!column.IsAutoIncrement && column.IsKey)
                {
                    attsString.AppendLine(string.Format(attrFormat, "DatabaseGenerated(DatabaseGeneratedOption.None)"));
                }
            }

            return attsString.ToString();
        }

        /// <summary>
        /// 获取model属性名称
        /// </summary>
        /// <param name="column">列名</param>
        /// <returns>model属性名称</returns>
        public virtual string GetPropertyName(string column)
        {
            if (string.IsNullOrEmpty(column)) throw new ArgumentNullException("column");
            string name = column;
            if (name.Contains('_'))
            {
                var arr = name.Split('_');
                name = "";
                foreach (var s in arr)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        name = name + s.Substring(0, 1).ToUpper();
                        if (s.Length > 1) name = name + s.Substring(1, s.Length - 1);
                    }
                }
            }

            name.Replace('.', '_');
            char c = name[0];
            if ('0' <= c && c <= '9' || c == '#' || c == '$')
            {
                name = "_" + name;
            }

            return name;
        }

        /// <summary>
        /// 获取model属性源代码
        /// </summary>
        /// <param name="column">列信息</param>
        /// <returns>model属性源代码</returns>
        public virtual string GetPropertyCode(ColumnInfoModel column)
        {
            if (column == null) throw new ArgumentNullException("column");
            string type = this.GetPropertyType(column);
            string propertyName = this.GetPropertyName(column.Name);

            return string.Format(this.GetPropertyFormat(), type ?? "", propertyName ?? ""); 
        }

        /// <summary>
        /// 获取model属性类型string
        /// </summary>
        /// <param name="column">列信息</param>
        /// <returns>model属性类型string</returns>
        public abstract string GetPropertyType(ColumnInfoModel column);

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            modelFormat = null;
            modelPropertyFormat = null;
            modelAttribute = null;
        }

        private string modelFormat;
        /// <summary>
        /// GetDbContextFormat
        /// </summary>
        /// <returns></returns>
        protected virtual string GetDbContextFormat()
        {
            if (string.IsNullOrEmpty(this.modelFormat))
            {
                try
                {
                    string file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ModelFile);
                    string path = System.IO.Path.GetDirectoryName(file);
                    if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                    if (!System.IO.File.Exists(file))
                    {
                        System.IO.File.WriteAllText(file, ModelFormat, Encoding.UTF8);
                    }
                    else
                    {
                        this.modelFormat = System.IO.File.ReadAllText(file, Encoding.UTF8);
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(this.modelFormat)) this.modelFormat = ModelFormat;
            }

            return this.modelFormat;
        }

        private string modelPropertyFormat;
        /// <summary>
        /// GetPropertyFormat
        /// </summary>
        /// <returns></returns>
        protected string GetPropertyFormat()
        {
            if (string.IsNullOrEmpty(this.modelPropertyFormat))
            {
                try
                {
                    string file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ModelPropertyFile);
                    string path = System.IO.Path.GetDirectoryName(file);
                    if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                    if (!System.IO.File.Exists(file))
                    {
                        System.IO.File.WriteAllText(file, ModelPropertyFormat, Encoding.UTF8);
                    }
                    else
                    {
                        this.modelPropertyFormat = System.IO.File.ReadAllText(file, Encoding.UTF8);
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(this.modelPropertyFormat)) this.modelPropertyFormat = ModelPropertyFormat;
            }

            return this.modelPropertyFormat;
        }

        private string modelAttribute;
        /// <summary>
        /// GetPropertyAttribute
        /// </summary>
        /// <returns></returns>
        protected virtual string GetPropertyAttribute()
        {
            if (string.IsNullOrEmpty(this.modelAttribute))
            {
                try
                {
                    string file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ModelAttributeFile);
                    string path = System.IO.Path.GetDirectoryName(file);
                    if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                    if (!System.IO.File.Exists(file))
                    {
                        System.IO.File.WriteAllText(file, ModelAttributeFormat, Encoding.UTF8);
                    }
                    else
                    {
                        this.modelAttribute = System.IO.File.ReadAllText(file, Encoding.UTF8);
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(this.modelAttribute)) this.modelAttribute = ModelAttributeFormat;
            }

            return this.modelAttribute;
        }

        private const string ModelAttributeFile = "template\\ModelAttribute.tmpl";
        private const string ModelPropertyFile = "template\\ModelProperty.tmpl";
        private const string ModelFile = "template\\Model.tmpl";

        private const string ModelAttributeFormat = @"        [{0}]";

        private const string ModelPropertyFormat = @"        public {0} {1} {{ get; set; }}";

        private const string ModelFormat = @"using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace {0}
{{
    [Table(""{1}"")]
    public partial class {2}
    {{
{3}
    }}
}}";
    }
}
