// NAntContrib
// Copyright (C) 2002 Tomas Restrepo (tomasr@mvps.org)
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.Collections;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Web.Mail;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

using NAnt.Contrib.Util;

namespace NAnt.Contrib.Tasks {
    /// <summary>
    /// A task to execute arbitrary SQL statements against a OLEDB data source.
    /// </summary>
    /// <remarks>
    /// You can specify a set of sql statements inside the
    /// sql element, or execute them from a text file that contains them. You can also
    /// choose to execute the statements in a single batch, or execute them one by one
    /// (even inside a transaction, if you want to).
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Execute a set of statements inside a transaction.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <sql
    ///     connstring="Provider=SQLOLEDB;Data Source=localhost; Initial Catalog=Pruebas; Integrated Security=SSPI"
    ///     transaction="true"
    ///     delimiter=";"
    ///     delimstyle="Normal"
    /// >
    ///     INSERT INTO jobs (job_desc, min_lvl, max_lvl) VALUES('My Job', 22, 45);
    ///     INSERT INTO jobs (job_desc, min_lvl, max_lvl) VALUES('Other Job', 09, 43);
    ///     SELECT * FROM jobs;
    /// </sql>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Execute a set of statements from a file and write all query results 
    ///   to a file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <sql
    ///     connstring="Provider=SQLOLEDB;Data Source=localhost; Initial Catalog=Pruebas; Integrated Security=SSPI"
    ///     transaction="true"
    ///     delimiter=";"
    ///     delimstyle="Normal"
    ///     print="true"
    ///     source="sql.txt"
    ///     output="${outputdir}/results.txt"
    /// />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Execute a SQL script generated by SQL Server Enterprise Manager.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <sql
    ///     connstring="Provider=SQLOLEDB;Data Source=localhost; Initial Catalog=Pruebas; Integrated Security=SSPI"
    ///     transaction="true"
    ///     delimiter="GO"
    ///     delimstyle="Line"
    ///     print="true"
    ///     source="pubs.xml"
    ///     batch="false"
    ///     output="${outputdir}/results.txt"
    /// />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("sql")]
    public class SqlTask : Task {
        #region Private Instance Fields

        private string _connectionString;
        private Encoding _encoding;
        private string _source;
        private string _delimiter;
        private DelimiterStyle _delimiterStyle = DelimiterStyle.Normal;
        private bool _print;
        private bool _useTransaction = true;
        private string _output;
        public string _embeddedSqlStatements;
        private bool _batch = true;
        private int _commandTimeout;
        private TextWriter _outputWriter;
        private bool _expandProps = true;
        private bool _append;
        private bool _showHeaders = true;
        private string _quoteChar = string.Empty;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Connection string used to access database.
        /// This should be an OleDB connection string.
        /// </summary>
        [TaskAttribute("connstring", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string ConnectionString  {
            get { return _connectionString; }
            set { _connectionString = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The encoding of the files containing SQL statements. The default is
        /// the system's current ANSI code page.
        /// </summary>
        [TaskAttribute("encoding")]
        public Encoding Encoding {
            get {
                if (_encoding == null) {
                    _encoding = Encoding.Default;
                }
                return _encoding;
            }
            set { _encoding = value; }
        }

        /// <summary>
        /// File where the sql statements are defined.
        /// </summary>
        /// <remarks>
        /// You cannot specify both a source and an inline set of statements.
        /// </remarks>
        [TaskAttribute("source")]
        public string Source {
            get { return (_source != null) ? Project.GetFullPath(_source) : null; }
            set { _source = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// String that separates statements from one another.
        /// </summary>
        [TaskAttribute("delimiter", Required=true)]
        public string Delimiter {
            get { return _delimiter; }
            set { _delimiter = value; }
        }

        /// <summary>
        /// If true, the statements will be executed as a single batch.
        /// If false, they will be executed one by one. Default is true.
        /// </summary>
        [TaskAttribute("batch")]
        [BooleanValidator()]
        public bool Batch {
            get { return _batch; }
            set { _batch = value; }
        }

        /// <summary>
        /// If true, the any nant-style properties on the sql will be
        /// expanded before execution. Default is true.
        /// </summary>
        [TaskAttribute("expandprops")]
        [BooleanValidator()]
        public bool ExpandProperties {
            get { return _expandProps; }
            set { _expandProps = value; }
        }

        /// <summary>
        /// Command timeout to use when creating commands.
        /// </summary>
        [TaskAttribute("cmdtimeout")]
        public int CommandTimeout {
            get { return _commandTimeout; }
            set { _commandTimeout = value; }
        }

        /// <summary>
        /// Kind of delimiter used. Allowed values are Normal or Line.
        /// </summary>
        /// <remarks>
        /// Delimiters can be of two kinds: Normal delimiters are
        /// always specified inline, so they permit having two
        /// different statements in the same line. Line delimiters,
        /// however, need to be in a line by their own.
        /// Default is Normal.
        /// </remarks>
        [TaskAttribute("delimstyle", Required=true)]
        public DelimiterStyle DelimiterStyle {
            get { return _delimiterStyle; }
            set { _delimiterStyle = value; }
        }

        /// <summary>
        /// If set to true, results from the statements will be
        /// output to the build log.
        /// </summary>
        [TaskAttribute("print")]
        [BooleanValidator()]
        public bool Print {
            get { return _print; }
            set { _print = value; }
        }

        /// <summary>
        /// If set, the results from the statements will be output to the 
        /// specified file.
        /// </summary>
        [TaskAttribute("output")]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { _output = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// If set to <see langword="true" />, all statements will be executed
        /// within a single transaction. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("transaction")]
        [BooleanValidator()]
        public bool UseTransaction {
            get { return _useTransaction; }
            set { _useTransaction = value; }
        }

        /// <summary>
        /// Whether output should be appended to or overwrite
        /// an existing file. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("append")]
        [BooleanValidator()]
        public bool Append {
            get { return _append; }
            set { _append = value; }
        }

        /// <summary>
        /// If set to <see langword="true" />, prints headers for result sets.
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("showheaders")]
        [BooleanValidator()]
        public bool ShowHeaders {
            get { return _showHeaders; }
            set { _showHeaders = value; }
        }

        /// <summary>
        /// The character(s) to surround result columns with when printing, the 
        /// default is an empty string.
        /// </summary>
        [TaskAttribute("quotechar")]
        public string QuoteChar {
            get { return _quoteChar; }
            set { _quoteChar = value; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the underlying <see cref="TextWriter" /> to which output will 
        /// be written if <see cref="Output" /> is set.
        /// </summary>
        /// <value>
        /// A <see cref="TextWriter" /> for the file specified in <see cref="Output" />,
        /// or <see langword="null" /> if <see cref="Output" /> is not set.
        /// </value>
        protected TextWriter OutputWriter {
            get { return _outputWriter; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes task and ensures the supplied attributes are valid.
        /// </summary>
        protected override void Initialize() {
            _embeddedSqlStatements = ((XmlElement) XmlNode).InnerText;
            if (Source == null && String.IsNullOrEmpty(_embeddedSqlStatements)) {
                throw new BuildException("No source file or statements have been specified.", 
                    Location);
            }
        }

        /// <summary>
        /// This is where the work is done.
        /// </summary>
        protected override void ExecuteTask() {
            if (Output != null) {
                try  {
                    if (Append) {
                        _outputWriter = File.AppendText(Output);
                    } else {
                        _outputWriter = File.CreateText(Output);
                    }
                }  catch (IOException ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Cannot open output file '{0}'.", Output), Location, ex);
                }
            } else {
                _outputWriter = new LogWriter(this, Level.Info, CultureInfo.InvariantCulture);
            }

            if (Verbose) {
                OutputWriter.WriteLine("Connection String: " + ConnectionString);
                OutputWriter.WriteLine("Use Transaction: " + UseTransaction.ToString(CultureInfo.InvariantCulture));
                OutputWriter.WriteLine("Batch Sql Statements: " + this.Batch.ToString(CultureInfo.InvariantCulture));
                OutputWriter.WriteLine("Batch Delimiter: " + this.Delimiter);
                OutputWriter.WriteLine("Delimiter Style: " + this.DelimiterStyle.ToString(CultureInfo.InvariantCulture));
                OutputWriter.WriteLine("Fail On Error: " + this.FailOnError.ToString(CultureInfo.InvariantCulture));
                OutputWriter.WriteLine("Source script file: " + this.Source);
                OutputWriter.WriteLine("Output file: " + this.Output);
            }

            SqlHelper sqlHelper = new SqlHelper(ConnectionString, UseTransaction);
            sqlHelper.Connection.InfoMessage += new OleDbInfoMessageEventHandler(SqlMessageHandler);

            try {
                if (Batch) {
                    ExecuteStatementsInBatch(sqlHelper);
                } else {
                    ExecuteStatements(sqlHelper);
                }
            } catch (Exception ex) {
                sqlHelper.Close(false);
                throw new BuildException("Error while executing SQL statement.", Location, ex);
            }

            sqlHelper.Close(true);
            OutputWriter.Close();
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Executes the SQL Statements one by one.
        /// </summary>
        /// <param name="sqlHelper"></param>
        private void ExecuteStatements(SqlHelper sqlHelper) {
            SqlStatementList list = CreateStatementList();

            if (Source == null) {
                list.ParseSql(_embeddedSqlStatements);
            } else {
                list.ParseSqlFromFile(Source, Encoding);
            }

            foreach (string statement in list) {
                // only write messages to the build log if the OutputWriter is not
                // writing to the build log too
                if (Output != null) {
                    Log(Level.Verbose, "SQL Statement:");
                    Log(Level.Verbose, statement);
                }

                if (this.Verbose) {
                    OutputWriter.WriteLine();
                    OutputWriter.WriteLine("SQL Statement:");
                    OutputWriter.WriteLine(statement);
                }

                IDataReader results = null;

                try {
                    results = sqlHelper.Execute(statement, CommandTimeout);
                } catch (Exception ex) {
                    Log(Level.Error, "SQL Error: " + ex.Message);
                    Log(Level.Error, "Statement: " + statement);
                } finally {
                    ProcessResults(results, OutputWriter);
                }

            }
        }

        /// <summary>
        /// Executes the SQL statements in a single batch.
        /// </summary>
        /// <param name="sqlHelper"></param>
        private void ExecuteStatementsInBatch(SqlHelper sqlHelper) {
            SqlStatementList list = CreateStatementList();
            SqlStatementAdapter adapter = new SqlStatementAdapter(list);
        
            string sql = null;
            if (Source == null) {
                sql = adapter.AdaptSql(_embeddedSqlStatements);
            } else {
                sql = adapter.AdaptSqlFile(Source, Encoding);
            }

            // only write messages to the build log if the OutputWriter is not
            // writing to the build log too
            if (Output != null) {
                Log(Level.Verbose, "SQL Statement:");
                Log(Level.Verbose, sql);
            }

            if (this.Verbose) {
                OutputWriter.WriteLine();
                OutputWriter.WriteLine("SQL Statement:");
                OutputWriter.WriteLine(sql.Trim());
            }

            IDataReader results = sqlHelper.Execute(sql, CommandTimeout);
            ProcessResults(results, OutputWriter);
        }

        /// <summary>
        /// Process a result set.
        /// </summary>
        /// <param name="results">Result set.</param>
        /// <param name="writer"><see cref="TextWriter" /> to write output to.</param>
        private void ProcessResults(IDataReader results, TextWriter writer) {
            try {
                do {
                    if (ShowHeaders) {
                        // output header
                        DataTable schema = results.GetSchemaTable();
                        if (schema != null) {
                            writer.WriteLine();

                            int totalHeaderSize = 0;
                            foreach (DataRow row in schema.Rows) {
                                string columnName = row["ColumnName"].ToString();
                                writer.Write(columnName + new string(' ', 2));
                                totalHeaderSize += columnName.Length + 2;
                            }

                            writer.WriteLine();

                            if (totalHeaderSize > 2) {
                                writer.WriteLine(new String('-', totalHeaderSize - 2));
                            }
                        }
                    }
                
                    // holds a value indicating whether an empty separator line
                    // should be output
                    bool sepLine = false;

                    // output results
                    while (results.Read()) {
                        bool first = true;
                        StringBuilder line = new StringBuilder(100);
                        for (int i = 0; i < results.FieldCount; i++) {
                            if (first) {
                                first = false;
                            } else {
                                line.Append(", ");
                            }
                            line.Append(QuoteChar);
                            line.Append(results[i].ToString());
                            line.Append(QuoteChar);
                        }
                        // output result
                        writer.WriteLine(line.ToString());
                        // determine whether separator line should be output
                        sepLine = line.Length > 0;
                    }
                    if (sepLine) {
                        writer.WriteLine();
                    }
                } while (results.NextResult());
            } finally {
                results.Close();
            }

            if (results.RecordsAffected >= 0) {
                Log(Level.Info, "{0} records affected", results.RecordsAffected);
            }
        }

        private SqlStatementList CreateStatementList() {
            SqlStatementList list = new SqlStatementList(Delimiter, DelimiterStyle);
         
            if (ExpandProperties) {
                list.Properties = this.PropertyAccessor;
            }
            return list;
        }

        private void SqlMessageHandler(object sender, OleDbInfoMessageEventArgs e) {
            OutputWriter.WriteLine(e.Message);

            if (Output != null) {
                Log(Level.Verbose, e.Message);
            }
        }

        #endregion Private Instance Methods
    }
}
