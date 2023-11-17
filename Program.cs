using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AddLogger
{
    class Program
    {
        static List<String> pathfiles;

        static void Main(string[] args)
        {
            string currpath = String.Empty;  
            try
            {
                pathfiles = new List<string>();
                var ext = new List<string> { ".cpp", ".h", ".hpp" };

                pathfiles = Directory.GetFiles(args[0], "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s))).ToList();



                foreach (string path in pathfiles)
                {
                    string codelogging = String.Empty;
                    string sourcecode;
                    currpath = path;

                    using (StreamReader rd = new StreamReader(path, Encoding.UTF8))
                    {
                        sourcecode = rd.ReadToEnd();
                    }

                    codelogging = sourcecode;
                   // FindIf(path, ref codelogging);


                    if (AddLogToFailed(path, ref codelogging))
                    {
                        using (StreamWriter rw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            rw.Write(codelogging);
                        }
                    }

                    if (AddLogToCatch(path, ref codelogging))
                    {
                        using (StreamWriter rw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            rw.Write(codelogging);
                        }
                    }

                    if (AddTraceMethod(path, ref codelogging))
                    {
                        using (StreamWriter rw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            rw.Write(codelogging);
                        }
                    }

                    if (AddLogToE(path, ref codelogging))
                    {
                        using (StreamWriter rw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            rw.Write(codelogging);
                        }
                    }

                    if (ATLTRACEReplacer(path, ref codelogging))
                    {
                        using (StreamWriter rw = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            rw.Write(codelogging);
                        }
                    }
                    Console.WriteLine(path + " PARSED");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(currpath);
            }
          }

        private static bool AddLogToFailed(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ' };

            Match mtc = null;
            string method = String.Empty;
            bool modified = false;
            string methodname = String.Empty;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Count(); i++)
            {
                try
                {
                    string failed = lines[i];
                    if (String.IsNullOrWhiteSpace(failed))
                        continue;

                    string trimmedendfailed = failed.TrimEnd();
                    string trimmedfailed = failed.Trim();
                    string leading = trimmedendfailed.Replace(trimmedfailed, "");

                    if ((trimmedfailed.Contains("FAILED(hr)") || trimmedfailed.Contains("FAILED( hr )") || trimmedfailed.Contains("FAILED(hr )")))
                    {
                        if (i >= lines.Count() - 1)
                            continue;

                        string result = lines[i + 1];
                        string trimmedresult = result.Trim(trimmers);
                        if (String.IsNullOrEmpty(trimmedresult) || trimmedresult.StartsWith("//"))
                            continue;

                        if ((trimmedresult == "return hr;" || trimmedresult == "throw hr;"))
                        {
                            if (String.IsNullOrWhiteSpace(methodname))
                                continue;

                            if (trimmedresult.Contains("//Logged"))
                                continue;

                            StringBuilder sb = new StringBuilder();
                            sb.Append(method);
                            sb.Append("\n" + failed);
                            sb.Append("\n" + leading + "{");
                            sb.Append("\n" + leading + "\tLOG4CPLUS_ERROR(logcmps, LOG4CPLUS_TEXT(\"Failed to ");
                            string parsedmethodname = methodname.Replace("\"", "\\\"");
                            sb.Append(parsedmethodname);
                            sb.Append(" Error: \") << std::hex << hr);");
                            sb.Append("\n" + leading + "\t" + trimmedresult + "\t //Logged");
                            sb.Append("\n" + leading + "}");

                            bool b = codelogging.Contains(method + Environment.NewLine + failed + Environment.NewLine + result);
                            if (b)
                            {
                                codelogging = codelogging.Replace(method + Environment.NewLine + failed + Environment.NewLine + result, sb.ToString());
                                modified = true;
                                Console.WriteLine("###################################################################");
                                Console.WriteLine(method + Environment.NewLine + failed + Environment.NewLine + result);
                                Console.WriteLine("REPLACED WITH");
                                Console.WriteLine(sb.ToString());
                                Console.WriteLine("//////////////////////////////////////////////////////////////////");
                            }
                        }
                    }
                    else
                    {
                        methodname = String.Empty;
                        method = failed;
                        string trimmedmethod = method.Trim(trimmers);
                        mtc = Regex.Match(trimmedmethod, @"(?<=->)(.*)(?=\()");
                        if (mtc == null || String.IsNullOrWhiteSpace(mtc.Value))
                        {
                            mtc = Regex.Match(trimmedmethod, @"(?<=\=)(.*)(?=\()");
                        }
                        else if (mtc == null || String.IsNullOrWhiteSpace(mtc.Value))
                        {
                            mtc = Regex.Match(trimmedmethod, @"(?<=\= )(.*)(?=\()");
                        }

                        if (mtc != null)
                            methodname = (mtc.Value).Trim();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }

            return modified;
        }
        private static bool AddLogToCatch(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ' };

            string method = String.Empty;
            bool modified = false;
            string methodname = String.Empty;
            string[] lines = File.ReadAllLines(path);
            List<string> lstlines = File.ReadAllLines(path).ToList();

            int offset = 0;
        
            for (int i = 0; i < lines.Count(); i++)
            {
                try
                {
                    string catchstr = lines[i];

                    if (String.IsNullOrWhiteSpace(catchstr))
                        continue;

                    string trimmedendcatch = catchstr.TrimEnd();
                    string trimmedcatch = catchstr.Trim();
                    string leading = trimmedendcatch.Replace(trimmedcatch, "");

                    if (trimmedcatch.Contains("catch(...)") || trimmedcatch.Contains("catch( ... )"))
                    {
                        if (trimmedcatch.StartsWith("//"))
                            continue;

                        if (trimmedcatch.Contains("//Logged"))
                            continue;

                        string braceopen = String.Empty;
                        int k = i;
                        for (k = i; k < lines.Count(); k++) // in hce linea sta {
                        {
                            if (lines[k].Contains("{"))
                            {
                                break;
                            }
                        }

                        if (lines[k].Contains("//Logged"))
                            continue;

                        string[] split = lines[k].Split('{');
                        if (split != null)
                        {
                            lstlines.RemoveAt(k + offset);
                            lstlines.Insert(k + offset, split[0]+ "{ //Logged");
                            lstlines.Insert(k + offset + 1, leading + "\tLOG4CPLUS_ERROR(logcmps, LOG4CPLUS_TEXT(\"Unexpected Error\"));");
                            StringBuilder sb = new StringBuilder("\t");
                            for (int r = 1; r < split.Length; r++)
                            {
                                sb.Append(split[r]);
                            }
                            lstlines.Insert(k + offset + 2, leading + sb.ToString());
                            offset += 2;
                            modified = true;
                            Console.WriteLine("###################################################################");
                            Console.WriteLine(lines[k - 1] + Environment.NewLine + lines[k]);
                            Console.WriteLine("REPLACED WITH");
                            Console.WriteLine(lstlines[k + offset - 1] + Environment.NewLine + lstlines[k + offset] + Environment.NewLine + lstlines[k + offset + 1] + Environment.NewLine + lstlines[k + offset + 2]);
                            Console.WriteLine("//////////////////////////////////////////////////////////////////");

                        }

                    }


                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            codelogging = String.Join(Environment.NewLine, lstlines.ToArray());

            return modified;
        }
        private static bool AddTraceMethod(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ' };

            string method = String.Empty;
            bool modified = false;
            string methodname = String.Empty;

            string[] lines = File.ReadAllLines(path);
            List<string> lstlines = File.ReadAllLines(path).ToList();

            int offset = 0;
            for (int i = 0; i < lines.Count(); i++)
            {
                try
                {
                    method = lines[i];
                    if (String.IsNullOrWhiteSpace(method))
                        continue;

                    if (method.Contains("((CRApp::CRExportFormatType)31)"))
                        Console.WriteLine("");

                    string trimmedendmethod = method.TrimEnd(trimmers);
                    string trimmedmethod = method.Trim(trimmers);
                    string leading = trimmedendmethod.Replace(trimmedmethod, "");

                    if (!trimmedmethod.Contains("::"))
                        continue;

                    if (trimmedmethod.EndsWith(";") || trimmedmethod.Contains(";//") || trimmedmethod.Contains("; //"))
                        continue;

                    if (trimmedmethod.StartsWith("{"))
                        continue;


                    if (trimmedmethod.Contains("="))
                        continue;

                    if (trimmedmethod.Contains("}") && !trimmedmethod.Contains("{"))
                        continue;

                    if (trimmedmethod.StartsWith("public") || trimmedmethod.StartsWith("typedef") || trimmedmethod.StartsWith("#") || trimmedmethod.StartsWith("return"))
                        continue;

                    if (trimmedmethod.StartsWith("class"))
                        continue;

                    if (trimmedmethod.StartsWith("MESSAGE_HANDLER(") || trimmedmethod.StartsWith("MESSAGE_HANDLER ("))
                        continue;

                    string[] arr = trimmedmethod.Split();
                    for (int q = 0; q < arr.Count(); q++)
                    {
                        if (arr[q].Contains("::"))
                        {
                            methodname = arr[q];
                            string[] justname = methodname.Split('(');
                            if (justname == null || justname.Count() == 0)
                                break;

                            methodname = justname[0];
                        }
                    }

                    if (String.IsNullOrEmpty(methodname))
                        continue;

                    string braceopen = String.Empty;
                    int k = i;
                    bool notvalid = false;
                    for (k = i; k < lines.Count(); k++) // in hce linea sta {
                    {
                        if (lines[k].Contains("{"))
                        {
                            notvalid = true;
                            break;
                        }
                        if (lines[k].Contains("}"))
                        {
                            break;
                        }
                    }


                    if (!notvalid)
                        continue;

                    if (lines[k].Contains("//Logged"))
                        continue;

                    string[] split = lines[k].Split('{');
                    if (split != null)
                    {
                        lstlines.RemoveAt(k + offset);
                        lstlines.Insert(k + offset, split[0] + "{ //Logged");
                        StringBuilder sblog = new StringBuilder();
                        sblog.Append(leading + "\tLOG4CPLUS_TRACE(logcmps, LOG4CPLUS_TEXT(\"Enter in ");
                        sblog.Append(methodname);
                        sblog.Append("\"));");
                        lstlines.Insert(k + offset + 1, sblog.ToString());

                        StringBuilder sb = new StringBuilder("\t");
                        for (int r = 1; r < split.Length; r++)
                        {
                            sb.Append(split[r]);
                        }
                        lstlines.Insert(k + offset + 2, leading + sb.ToString());

                        modified = true;
                        Console.WriteLine("###################################################################");
                        Console.WriteLine(lines[k - 1] + Environment.NewLine + lines[k]);
                        Console.WriteLine("REPLACED WITH");
                        Console.WriteLine(lstlines[k + offset - 1] + Environment.NewLine + lstlines[k + offset] + Environment.NewLine + lstlines[k + offset + 1] + Environment.NewLine + lstlines[k + offset + 2]);
                        Console.WriteLine("//////////////////////////////////////////////////////////////////");
                        offset += 2;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            codelogging = String.Join(Environment.NewLine, lstlines.ToArray());
            return modified;
        }
        private static bool AddLogToE(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ' };
            bool modified = false;
            string statementError = String.Empty;
            string errorname = String.Empty;
            string[] arrlines = File.ReadAllLines(path);
            List<string> loggedlines = File.ReadAllLines(path).ToList();
          

            if (loggedlines.Count <=5) // non parso file corti
                return false;

            int offset = 0;
            int m = 0;
            for (int k = 0; k < arrlines.Length; k++)
            {
                try
                {

                    string statementBefore = String.Empty;
                    statementError = arrlines[k];
                    if (statementError == null)
                        break;

                    if (String.IsNullOrWhiteSpace(statementError.Trim()))
                        continue;

                    if (!statementError.Contains("return E") || statementError.Contains("//Logged") || statementError.Contains("{"))
                        continue;

                    string errorbegin = statementError.TrimEnd();
                    string errortrimmed = statementError.Trim();
                    string leading = errorbegin.Replace(errortrimmed, "");
                    bool stop = false;
                    bool addbraces = false;
                    for (int l = k - 1; l > 0; l--)
                    {
                        statementBefore = arrlines[l].Trim(trimmers);
                        //if (statementBefore.Contains("mainmenu.InsertMenu") || statementBefore.Contains("(ULONG)(newmenu.m_hMenu),menutext"))
                        //    Console.WriteLine("");

                        if (String.IsNullOrWhiteSpace(statementBefore) || statementBefore.StartsWith("//") || statementBefore.StartsWith("/*") || statementBefore.StartsWith("*/"))
                            continue;





                        if (statementBefore.Contains("if") && !statementBefore.EndsWith("{") )
                        {
                            addbraces = true;
                            break;
                        }
                        else if (statementBefore.Contains("if") && statementBefore.EndsWith("{"))
                        {
                            addbraces = false;
                            break;
                        }
                        if (statementBefore.Contains("else") && !statementBefore.EndsWith("{"))
                        {
                            addbraces = true;
                            break;
                        }
                        else if (statementBefore.Contains("else") && statementBefore.EndsWith("{"))
                        {
                            addbraces = false;
                            break;
                        }
                        else if (statementBefore.EndsWith(";"))
                        {
                            addbraces = false;
                            break;
                        }
                    }

                    if (stop)
                        continue;

                    string[] arr = errortrimmed.Split();
                    for (int i = 0; i < arr.Count(); i++)
                    {
                        Match mtch = Regex.Match(arr[i], @"(?<=E)(.*)(?=\;)");
                        if (!String.IsNullOrWhiteSpace(mtch.Value))
                        {
                            errorname = mtch.Value;
                        }
                    }

                    if (String.IsNullOrWhiteSpace(errorname))
                        continue;


                    m = k + offset;
                    StringBuilder sb = new StringBuilder();
                   
                    if (addbraces)
                    {
                        loggedlines.RemoveAt(m);
                        loggedlines.Insert(m , leading + "{");
                        if (errorname == "_NOTIMPL")
                            loggedlines.Insert(m+1 , leading + "\t \tLOG4CPLUS_DEBUG(logcmps, LOG4CPLUS_TEXT(\"Not implemented: \") <<std::hex<<E" + errorname + ");");
                        else
                            loggedlines.Insert(m +1 , leading + "\t \tLOG4CPLUS_ERROR(logcmps, LOG4CPLUS_TEXT(\"Failed with error:\") <<std::hex<<E" + errorname + ");");

                        loggedlines.Insert(m + 2, "\t" + statementError + " //Logged");
                        loggedlines.Insert(m + 3, leading + "}");

                        offset += 3;
                        Console.WriteLine("###################################################################");
                        Console.WriteLine(arrlines[k]);
                        Console.WriteLine("REPLACED WITH");
                        Console.WriteLine(loggedlines[m] + Environment.NewLine + loggedlines[m + 1] + Environment.NewLine + loggedlines[m + 2] + Environment.NewLine + loggedlines[m + 3]);
                        Console.WriteLine("//////////////////////////////////////////////////////////////////");
                    }
                    else
                    {
                        loggedlines.RemoveAt(m);
                        if (errorname == "_NOTIMPL")
                            loggedlines.Insert(m, leading + "\tLOG4CPLUS_DEBUG(logcmps, LOG4CPLUS_TEXT(\"Not implemented: \") <<std::hex<<E" + errorname + ");");
                        else
                            loggedlines.Insert(m, leading + "\tLOG4CPLUS_ERROR(logcmps, LOG4CPLUS_TEXT(\"Failed with error:\") <<std::hex<<E" + errorname + ");");
                        loggedlines.Insert(m + 1, "\t" + statementError + " //Logged");

                        Console.WriteLine(arrlines[k]);
                        Console.WriteLine("REPLACED WITH");
                        Console.WriteLine(loggedlines[m] + Environment.NewLine + loggedlines[m + 1]);
                        Console.WriteLine("//////////////////////////////////////////////////////////////////");

                        offset += 1;
                    }

                   
                    modified = true;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }

            codelogging = String.Join(Environment.NewLine, loggedlines.ToArray());
            return modified;
        }


        private static void FindIf(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ', '\n' };

            string statementif = String.Empty;
            bool modified = false;
            string errorname = String.Empty;
            string[] arr = File.ReadAllLines(path);
            for (int i = 0; i < arr.Count(); i++)
            {
                statementif = arr[i];
                if (statementif == null)
                    break;

                statementif = statementif.Trim(trimmers);
                if (String.IsNullOrWhiteSpace(statementif))
                    continue;

                if (!statementif.Contains("if") && !statementif.Contains("if ("))
                    continue;

                if (statementif.Contains("{"))
                    continue;

                int countOpen = statementif.Count(f => f == '(');
                int countClose = statementif.Count(f => f == ')');

                if (countOpen != countClose)
                {
                    Console.WriteLine("More lines + " + statementif);
                    Console.WriteLine(path + " PARSED");
                }

            }
        }


        private static bool ATLTRACEReplacer(string path, ref string codelogging)
        {
            char[] trimmers = new char[] { '\r', '\t', ' ', '\n' };

            string statementE = String.Empty;
            bool modified = false;
            string errorname = String.Empty;
            string[] arr = File.ReadAllLines(path);
            List<string> lst = File.ReadAllLines(path).ToList();


           
            for (int i = 0; i < arr.Length; i++)
            {
                statementE = arr[i];
                if (statementE == null)
                    break;

                if (String.IsNullOrWhiteSpace(statementE.Trim()))
                    continue;

                if (!statementE.Contains("ATLTRACE"))
                    continue;

                string trimmedendstatementE = statementE.TrimEnd();
                string trimmedstatementE = statementE.Trim();
                string leading = trimmedendstatementE.Replace(trimmedstatementE, "");

                Match mtch = Regex.Match(arr[i], @"(?<=ATLTRACE\()(.*)(?=\))");

                string matched = mtch.Value.Replace("\\n", "");
                matched = matched.Replace("\\r", "");

                if (String.IsNullOrWhiteSpace(matched))
                    continue;

                if (matched.Contains("%") || !matched.StartsWith("\""))
                    continue;

               
                lst.RemoveAt(i);
                StringBuilder sbrep = new StringBuilder();
                if (trimmedstatementE.Contains('{'))
                {
                    int start = trimmedstatementE.IndexOf("ATLTRACE(");
                    int end = trimmedstatementE.IndexOf("\");");
               

                    sbrep.Append("LOG4CPLUS_DEBUG(logcmps,_T(");
                    sbrep.Append(matched.Trim(trimmers));
                    sbrep.Append("));");

                    string aaa = String.Empty;
                    if (end + 3 < trimmedstatementE.Length)
                        aaa = trimmedstatementE.Substring(0, start) + sbrep.ToString() + trimmedstatementE.Substring(end + 3);
                    else
                        aaa = trimmedstatementE.Substring(0, start) + sbrep.ToString();
                        
                        
                        lst.Insert(i, aaa);

                        //lst.Insert(m+1, sbrep.ToString());

                        //offset += 1;




                } else
                {
                    sbrep.Append("LOG4CPLUS_DEBUG(logcmps,_T(");
                    sbrep.Append(matched.Trim(trimmers));
                    sbrep.Append("));");
                    lst.Insert(i, sbrep.ToString());
                }


             
                Console.WriteLine("###################################################################");
                Console.WriteLine(statementE);
                Console.WriteLine("REPLACED WITH");
                Console.WriteLine(lst[i]);
                Console.WriteLine("//////////////////////////////////////////////////////////////////");
                modified = true;











            }
            codelogging = String.Join(Environment.NewLine, lst);

            return modified;
        }

    

   
       

     

       
    }
}
