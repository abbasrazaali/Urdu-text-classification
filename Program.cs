using System;
using System.Text;
using System.Collections;
using System.IO;

namespace Urdu_Text_Classification
{
    class Program
    {
        public static Hashtable C1News = new Hashtable();
        public static Hashtable C2Sports = new Hashtable();
        public static Hashtable C3Finance = new Hashtable();
        public static Hashtable C4Culture = new Hashtable();
        public static Hashtable C5Consumer_Information = new Hashtable();
        public static Hashtable C6Personal_Communication = new Hashtable();
        public static Hashtable termFreq = new Hashtable();
        public static Hashtable testTermFreq = new Hashtable();
        public static Hashtable wordList = new Hashtable();
        public static Hashtable stopWord = new Hashtable();
        public static Hashtable prefix = new Hashtable();
        public static Hashtable suffix = new Hashtable();
        public static Hashtable lexicon = new Hashtable();
        public static Hashtable maxTFlexicon = new Hashtable();
        public static Hashtable svmWordList = new Hashtable();
        public static double [] prior = new double[6];
        public static double [] classToken = new double[6];

        static void Main(string[] args)
        {
            try
            {
                loadStopWords("..\\..\\data\\StopWord.txt");
                loadWordList("..\\..\\data\\WordList.txt");
                loadAffixes("..\\..\\data\\Prefix.txt", "..\\..\\data\\svmWordList.txt");
                loadSVMWordList("..\\..\\data\\svmWordList.txt");
                
                // SVM
                //svmPreprocessor("..\\..\\data\\trainingCorpus", "..\\..\\data\\svmTrain.txt");
                //svmPreprocessor("..\\..\\data\\testCorpus", "..\\..\\data\\svmTest.txt");
                svmPreprocessor("..\\..\\data\\trainingCorpus", "D:\\MS\\Research Papers\\Urdu Text Classification\\Implementation\\SVM Light for Multiclass\\train.txt");
                svmPreprocessor("..\\..\\data\\testCorpus", "D:\\MS\\Research Papers\\Urdu Text Classification\\Implementation\\SVM Light for Multiclass\\test.txt");
                
                /*
                // Naive Bayes
                calc_prior("..\\..\\data\\trainingCorpus");
                // basline
                training("..\\..\\data\\trainingCorpus", false, false);
                testing("..\\..\\data\\testCorpus", "..\\..\\data\\NBAccuracy.txt", false, false, false, "Basline system");
                // eliminate stopwords
                training("..\\..\\data\\trainingCorpus", true, false);
                testing("..\\..\\data\\testCorpus", "..\\..\\data\\NBAccuracy.txt", true, false, true, "Stopword elimination and no stemming");
                // stemming
                training("..\\..\\data\\trainingCorpus", true, false);
                testing("..\\..\\data\\testCorpus", "..\\..\\data\\NBAccuracy.txt", false, true, true, "No stopword elimination and stemming");
                // eliminate stopwords and stemming
                training("..\\..\\data\\trainingCorpus", true, true);
                testing("..\\..\\data\\testCorpus", "..\\..\\data\\NBAccuracy.txt", true, true, true, "Stopword elimination and stemming");
                */
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
            }
        }

        // SVM preprocessor
        private static bool svmPreprocessor(String corpusFolder, String svmTrain)
        {
            String fileContents = null;
            String[] tokens;
            DirectoryInfo dir = new DirectoryInfo(corpusFolder);
            DirectoryInfo[] dirs = dir.GetDirectories();
            double[] freq = new double[100000];
            double[] max_freq = new double[100000];
            int cls = 1;
            uint l = 0, filelen;
            StreamReader fileRead;

            try
            {
                svmMaxFreq(corpusFolder);

                StreamWriter saveWriter = new StreamWriter(svmTrain);
                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");

                    filelen = (uint) files.Length / 4;
                    for (l = 0; l < filelen; l++)
                    {
                        fileRead = new StreamReader(new FileStream(files[l].Directory + "\\" + files[l].Name, FileMode.Open), Encoding.Unicode);
                        fileContents = fileRead.ReadToEnd().ToLower().Trim();
                        fileRead.Close();
                        fileRead.Dispose();

                        // normalization
                        fileContents = normalize(fileContents);

                        // tokenization
                        tokens = fileContents.Split('\r', '\n', '\t', '\0', '\f', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                            '`', '~', ' ', '!', '@', '#', '+', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '\'', '|', '\"', ';', ':', '/', '\\', '?', '>', '<', '.', ',', '-', '_', ' ', '¬', '', '�', 'ـ',
                            '۔', '،', '؟');

                        if (dr.Name == "News")
                            cls = 1;
                        else if (dr.Name == "Sports")
                            cls = 2;
                        else if (dr.Name == "Finance")
                            cls = 3;
                        else if (dr.Name == "Culture")
                            cls = 4;
                        else if (dr.Name == "Consumer Information")
                            cls = 5;
                        else if (dr.Name == "Personal Communication")
                            cls = 6;

                        for (long i = 0; i < tokens.Length; i++)
                        {
                            // stop word elimination and stemming
                            tokens[i] = unDiacritize(tokens[i].Trim());
                            if (tokens[i] != null)
                            {
                                tokens[i] = tokens[i].Trim();
                                if (wordList.ContainsKey(tokens[i].Trim()) && svmWordList.ContainsKey(tokens[i].Trim()) && !stopWord.ContainsKey(tokens[i]))
                                {
                                    if (!lexicon.ContainsKey(svmWordList[tokens[i]]))
                                    {
                                        lexicon.Add(svmWordList[tokens[i]], 1);
                                        freq[Convert.ToInt32(svmWordList[tokens[i]])] = 1.0;
                                        max_freq[Convert.ToInt32(svmWordList[tokens[i]])] = Convert.ToInt32(maxTFlexicon[tokens[i]]);
                                    }
                                    else
                                        freq[Convert.ToInt32(svmWordList[tokens[i]])]++;
                                }
                            }
                        }
                        if (lexicon.Count != 0)
                        {
                            saveWriter.Write(cls.ToString());
                            for (long j = 0; j < freq.Length; j++)
                            {
                                if (freq[j] > 0.0)
                                {
                                    saveWriter.Write(" " + j.ToString() + ":" + Convert.ToDouble(freq[j]) / Convert.ToDouble(max_freq[j]));
                                    freq[j] = 0.0;
                                }
                            }
                            saveWriter.Write(saveWriter.NewLine);
                        }
                        lexicon.Clear();
                    }
                }               
                saveWriter.Close();

                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // SVM max frqequency
        private static bool svmMaxFreq(String corpusFolder)
        {
            String fileContents = null;
            String[] tokens;
            DirectoryInfo dir = new DirectoryInfo(corpusFolder);
            DirectoryInfo[] dirs = dir.GetDirectories();
            double[] index = new double[100000];
            StreamReader fileRead;
            
            try
            {
                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");

                    foreach (FileInfo file in files)
                    {
                        fileRead = new StreamReader(new FileStream(file.Directory + "\\" + file.Name, FileMode.Open), Encoding.Unicode);
                        fileContents = fileRead.ReadToEnd().ToLower().Trim();
                        fileRead.Close();
                        fileRead.Dispose();

                        // normalization
                        fileContents = normalize(fileContents);

                        // tokenization
                        tokens = fileContents.Split('\r', '\n', '\t', '\0', '\f', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                            '`', '~', ' ', '!', '@', '#', '+', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '\'', '|', '\"', ';', ':', '/', '\\', '?', '>', '<', '.', ',', '-', '_', ' ', '¬', '', '�', 'ـ',
                            '۔', '،', '؟');

                        for (long i = 0; i < tokens.Length; i++)
                        {
                            // stop word elimination and stemming
                            tokens[i] = unDiacritize(tokens[i].Trim());
                            if (tokens[i] != null)
                            {
                                tokens[i] = tokens[i].Trim();
                                if (wordList.ContainsKey(tokens[i]) && !stopWord.ContainsKey(tokens[i]))
                                {
                                    if (!lexicon.ContainsKey(tokens[i]))
                                        lexicon.Add(tokens[i], 1);
                                    else
                                        lexicon[tokens[i]] = Convert.ToInt32(lexicon[tokens[i]]) + 1;
                                }
                            }
                        }
                        if (lexicon.Count != 0)
                        {
                            IDictionaryEnumerator e = lexicon.GetEnumerator();
                            while (e.MoveNext())
                            {
                                if (maxTFlexicon.ContainsKey(e.Key.ToString()) && Convert.ToInt32(maxTFlexicon[e.Key.ToString()]) < Convert.ToInt32(e.Value))
                                    maxTFlexicon[e.Key.ToString()] = Convert.ToInt32(e.Value);
                                else if (!maxTFlexicon.ContainsKey(e.Key.ToString()))
                                    maxTFlexicon.Add(e.Key.ToString(), Convert.ToInt32(e.Value));
                            }
                        }
                        lexicon.Clear();
                    }
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }
        
        // Nive Bayes training
        private static bool training(String corpusFolder, bool isStopwords, bool isStem)
        {
            long i = 0;
            String fileContents = null;
            String[] tokens;
            DirectoryInfo dir = new DirectoryInfo(corpusFolder);
            DirectoryInfo[] dirs = dir.GetDirectories();
            StreamReader fileRead;

            try
            {
                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");

                    foreach (FileInfo file in files)
                    {
                        fileRead = new StreamReader(new FileStream(file.Directory + "\\" + file.Name, FileMode.Open), Encoding.Unicode);
                        fileContents = fileRead.ReadToEnd().ToLower().Trim();
                        fileRead.Close();
                        fileRead.Dispose();

                        // normalization
                        fileContents = normalize(fileContents);

                        // tokenization
                        tokens = fileContents.Split('\r', '\n', '\t', '\0', '\f', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                            '`', '~', ' ', '!', '@', '#', '+', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '\'', '|', '\"', ';', ':', '/', '\\', '?', '>', '<', '.', ',', '-', '_', ' ', '¬', '', '�', 'ـ',
                            '۔', '،', '؟');

                        for (i = 0; i < tokens.Length; i++)
                        {
                            // stemming
                            if(isStem)
                                tokens[i] = stem(unDiacritize(tokens[i].Trim()));
                            else
                                tokens[i] = unDiacritize(tokens[i].Trim());

                            if (tokens[i] != null)
                            {
                                tokens[i] = tokens[i].Trim();
                                if (wordList.ContainsKey(tokens[i]) && (!isStopwords || !stopWord.ContainsKey(tokens[i])))
                                {
                                    if (!termFreq.ContainsKey(tokens[i]))
                                        termFreq.Add(tokens[i], 1);
                                    else
                                        termFreq[tokens[i]] = Convert.ToInt32(termFreq[tokens[i]]) + 1;

                                    if (dr.Name == "News" && !C1News.ContainsKey(tokens[i].Trim())) C1News.Add(tokens[i].Trim(), 1); else if (dr.Name == "News") C1News[tokens[i]] = Convert.ToInt32(C1News[tokens[i]]) + 1;

                                    if (dr.Name == "Sports" && !C2Sports.ContainsKey(tokens[i].Trim())) C2Sports.Add(tokens[i].Trim(), 1); else if (dr.Name == "Sports") C2Sports[tokens[i]] = Convert.ToInt32(C2Sports[tokens[i]]) + 1;

                                    if (dr.Name == "Finance" && !C3Finance.ContainsKey(tokens[i].Trim())) C3Finance.Add(tokens[i].Trim(), 1); else if (dr.Name == "Finance") C3Finance[tokens[i]] = Convert.ToInt32(C3Finance[tokens[i]]) + 1;

                                    if (dr.Name == "Culture" && !C4Culture.ContainsKey(tokens[i].Trim())) C4Culture.Add(tokens[i].Trim(), 1); else if (dr.Name == "Culture") C4Culture[tokens[i]] = Convert.ToInt32(C4Culture[tokens[i]]) + 1;

                                    if (dr.Name == "Consumer Information" && !C5Consumer_Information.ContainsKey(tokens[i].Trim())) C5Consumer_Information.Add(tokens[i].Trim(), 1); else if (dr.Name == "Consumer Information") C5Consumer_Information[tokens[i]] = Convert.ToInt32(C5Consumer_Information[tokens[i]]) + 1;

                                    if (dr.Name == "Personal Communication" && !C6Personal_Communication.ContainsKey(tokens[i].Trim())) C6Personal_Communication.Add(tokens[i].Trim(), 1); else if (dr.Name == "Personal Communication") C6Personal_Communication[tokens[i]] = Convert.ToInt32(C6Personal_Communication[tokens[i]]) + 1;
                                }
                            }
                        }
                    }
                }

                IDictionaryEnumerator e = termFreq.GetEnumerator();
                while (e.MoveNext())
                {
                    if (C1News.ContainsKey(e.Key.ToString())) C1News[e.Key.ToString()] = Convert.ToDouble(C1News[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C1News.Add(e.Key.ToString(), (Convert.ToDouble(C1News[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));

                    if (C2Sports.ContainsKey(e.Key.ToString())) C2Sports[e.Key.ToString()] = Convert.ToDouble(C2Sports[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C2Sports.Add(e.Key.ToString(), (Convert.ToDouble(C2Sports[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));

                    if (C3Finance.ContainsKey(e.Key.ToString())) C3Finance[e.Key.ToString()] = Convert.ToDouble(C3Finance[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C3Finance.Add(e.Key.ToString(), (Convert.ToDouble(C3Finance[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));

                    if (C4Culture.ContainsKey(e.Key.ToString())) C4Culture[e.Key.ToString()] = Convert.ToDouble(C4Culture[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C4Culture.Add(e.Key.ToString(), (Convert.ToDouble(C4Culture[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));

                    if (C5Consumer_Information.ContainsKey(e.Key.ToString())) C5Consumer_Information[e.Key.ToString()] = Convert.ToDouble(C5Consumer_Information[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C5Consumer_Information.Add(e.Key.ToString(), (Convert.ToDouble(C5Consumer_Information[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));

                    if (C6Personal_Communication.ContainsKey(e.Key.ToString())) C6Personal_Communication[e.Key.ToString()] = Convert.ToDouble(C6Personal_Communication[e.Key.ToString()]) / Convert.ToDouble(termFreq[e.Key.ToString()]); else C6Personal_Communication.Add(e.Key.ToString(), (Convert.ToDouble(C6Personal_Communication[e.Key.ToString()]) + 1) / (Convert.ToDouble(termFreq[e.Key.ToString()]) + termFreq.Count));
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // testing
        private static bool testing(String corpusFolder, String accuracyFile, bool isStopwords, bool isStem, bool isAppend, String desc)
        {
            long i = 0, j = 0, k = 0, max = 0;
            String fileContents = null;
            String[] tokens;
            DirectoryInfo dir = new DirectoryInfo(corpusFolder);
            DirectoryInfo[] dirs = dir.GetDirectories();
            double[] maxClass = new double[6];
            Int32[] correct = new Int32[6];
            Int32[] incorrect = new Int32[6];
            IDictionaryEnumerator e;
            StreamReader fileRead;
            StreamWriter fileWrite;

            try
            {
                if(isAppend)
                    fileWrite = new StreamWriter(accuracyFile, isAppend);
                else
                    fileWrite = new StreamWriter(accuracyFile);

                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");

                    foreach (FileInfo file in files)
                    {
                        fileRead = new StreamReader(new FileStream(file.Directory + "\\" + file.Name, FileMode.Open), Encoding.Unicode);
                        fileContents = fileRead.ReadToEnd().ToLower().Trim();
                        fileRead.Close();
                        fileRead.Dispose();

                        // normalization
                        fileContents = normalize(fileContents);

                        // tokenization
                        tokens = fileContents.Split('\r', '\n', '\t', '\0', '\f', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                            '`', '~', ' ', '!', '@', '#', '+', '$', '%', '^', '&', '*', '(', ')', '{', '}', '[', ']', '\'', '|', '\"', ';', ':', '/', '\\', '?', '>', '<', '.', ',', '-', '_', ' ', '¬', '', '�', 'ـ',
                            '۔', '،', '؟');

                        for (i = 0; i < tokens.Length; i++)
                        {
                            // stemming
                            if(isStem)
                                tokens[i] = stem(unDiacritize(tokens[i].Trim()));
                            else
                                tokens[i] = unDiacritize(tokens[i].Trim());

                            if (tokens[i] != null)
                            {
                                tokens[i] = tokens[i].Trim();
                                if (wordList.ContainsKey(tokens[i]) && (!isStopwords || !stopWord.ContainsKey(tokens[i])))
                                {
                                    if (!termFreq.ContainsKey(tokens[i]))
                                        termFreq.Add(tokens[i], 1);
                                    else
                                        termFreq[tokens[i]] = Convert.ToInt32(termFreq[tokens[i]]) + 1;
                                }
                            }
                        }

                        maxClass[0] = Math.Log10(prior[0]);
                        maxClass[1] = Math.Log10(prior[1]);
                        maxClass[2] = Math.Log10(prior[2]);
                        maxClass[3] = Math.Log10(prior[3]);
                        maxClass[4] = Math.Log10(prior[4]);
                        maxClass[5] = Math.Log10(prior[5]);

                        e = termFreq.GetEnumerator();
                        while (e.MoveNext())
                        {
                            if (C1News.ContainsKey(e.Key.ToString()))
                                maxClass[0] = maxClass[0] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C1News[e.Key.ToString()]));

                            if (C2Sports.ContainsKey(e.Key.ToString()))
                                maxClass[1] = maxClass[1] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C2Sports[e.Key.ToString()]));

                            if (C3Finance.ContainsKey(e.Key.ToString()))
                                maxClass[2] = maxClass[2] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C3Finance[e.Key.ToString()]));

                            if (C4Culture.ContainsKey(e.Key.ToString()))
                                maxClass[3] = maxClass[3] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C4Culture[e.Key.ToString()]));

                            if (C5Consumer_Information.ContainsKey(e.Key.ToString()))
                                maxClass[4] = maxClass[4] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C5Consumer_Information[e.Key.ToString()]));

                            if (C6Personal_Communication.ContainsKey(e.Key.ToString()))
                                maxClass[5] = maxClass[5] + Convert.ToDouble(e.Value) * Math.Log10(Convert.ToDouble(C6Personal_Communication[e.Key.ToString()]));
                        }
                        max = 0;
                        for (i = 1; i < maxClass.Length; i++)
                        {
                            if (maxClass[i] > maxClass[max])
                                max = i;
                        }

                        if (dr.Name == "News" && max == 0)
                            correct[0]++;
                        else if (dr.Name == "News" && max != 0)
                            incorrect[0]++;

                        else if (dr.Name == "Sports" && max == 1)
                            correct[1]++;
                        else if (dr.Name == "Sports" && max != 1)
                            incorrect[1]++;

                        else if (dr.Name == "Finance" && max == 2)
                            correct[2]++;
                        else if (dr.Name == "Finance" && max != 2)
                            incorrect[2]++;

                        else if (dr.Name == "Culture" && max == 3)
                            correct[3]++;
                        else if (dr.Name == "Culture" && max != 3)
                            incorrect[3]++;

                        else if (dr.Name == "Consumer Information" && max == 4)
                            correct[4]++;
                        else if (dr.Name == "Consumer Information" && max != 4)
                            incorrect[4]++;

                        else if (dr.Name == "Personal Communication" && max == 5)
                            correct[5]++;
                        else if (dr.Name == "Personal Communication" && max != 5)
                            incorrect[5]++;

                        termFreq.Clear();
                    }
                }

                fileWrite.WriteLine(desc);
                fileWrite.WriteLine("Class\t\tCorrect\t\tIncorrect");
                for (i = 0; i < maxClass.Length; i++)
                {
                    if(i == 0)
                        fileWrite.WriteLine("News" + "\t\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());
                    else if (i == 1)
                        fileWrite.WriteLine("Sports" + "\t\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());
                    else if (i == 2)
                        fileWrite.WriteLine("Finance" + "\t\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());
                    else if (i == 3)
                        fileWrite.WriteLine("Culture" + "\t\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());
                    else if (i == 4)
                        fileWrite.WriteLine("Consumer Info." + "\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());
                    else if (i == 5)
                        fileWrite.WriteLine("Personal Comm." + "\t" + correct[i].ToString() + "\t\t" + incorrect[i].ToString());

                    j += correct[i];
                    k += incorrect[i];
                }
                fileWrite.WriteLine("Total\t\t" + j.ToString() + "\t\t" + k.ToString());
                fileWrite.WriteLine("Accuracy\t\t" + ((Convert.ToDouble(j) / Convert.ToDouble(j + k)) * 100.0).ToString() + "%");
                fileWrite.WriteLine("Error\t\t" + ((Convert.ToDouble(k) / Convert.ToDouble(j + k)) * 100.0).ToString() + "%");
                fileWrite.WriteLine(Environment.NewLine + Environment.NewLine);

                fileWrite.Close();

                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // prior
        private static bool calc_prior(String corpusFile)
        {
            double docs = 0;

            try
            {
                DirectoryInfo dir = new DirectoryInfo(corpusFile);
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");
                    docs += files.Length;
                }

                foreach (DirectoryInfo dr in dirs)
                {
                    FileInfo[] files = dr.GetFiles("*.txt");
                    if (dr.Name == "News")
                        prior[0] = files.Length / docs;
                    else if (dr.Name == "Sports")
                        prior[1] = files.Length / docs;
                    else if (dr.Name == "Finance")
                        prior[2] = files.Length / docs;
                    else if (dr.Name == "Culture")
                        prior[3] = files.Length / docs;
                    else if (dr.Name == "Consumer Information")
                        prior[4] = files.Length / docs;
                    else if (dr.Name == "Personal Communication")
                        prior[5] = files.Length / docs;
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }
        
        // stemming
        private static String stem(String word)
        {
            int i = 0;
            String pref = null, suff = null, pref_root = null, root_suff = null;

            try
            {
                if (word != null)
                {
                    for (i = 0; i < word.Length; i++)
                    {
                        pref += word[i];
                        if ((String)prefix[pref] != null)
                        {
                            for (i = i + 1; i < word.Length; i++)
                                pref_root += word[i];

                            if (pref_root != null)
                                return pref_root;
                            else
                                return word;
                        }

                        suff = word.Substring(i);
                        if ((String)suffix[suff] != null)
                            if (root_suff != null)
                                return root_suff;
                            else
                                return word;
                        else
                            root_suff += word[i];
                    }
                }
                return word;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return null;
            }
        }

        // normalization
        private static String normalize(String str)
        {
            try
            {
                str = str.Replace("آ", "آ");
                str = str.Replace("أ", "أ");
                str = str.Replace("ؤ", "ؤ");
                str = str.Replace("ة", "ۃ");
                str = str.Replace("ـ", "");
                str = str.Replace("ك", "ک");
                str = str.Replace("ه", "ہ");
                str = str.Replace("ۂ", "ۂ");
                str = str.Replace("ۀ", "ۂ");
                str = str.Replace("ي", "ی");
                str = str.Replace("٠", "۰");
                str = str.Replace("١", "۱");
                str = str.Replace("٢", "۲");
                str = str.Replace("٣", "۳");
                str = str.Replace("٤", "۴");
                str = str.Replace("٥", "۵");
                str = str.Replace("٦", "۶");
                str = str.Replace("٧", "۷");
                str = str.Replace("٨", "۸");
                str = str.Replace("٩", "۹");
                str = str.Replace("ۓ", "ۓ");

                return str;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return null;
            }
        }

        // load word list
        private static bool loadWordList(String file)
        {
            try
            {
                String line = (File.OpenText(file)).ReadToEnd();
                (File.OpenText(file)).Close();

                String[] tokens = line.Split('\n', '\r');

                for (uint i = 0; i < tokens.Length; i += 2)
                {
                    if (unDiacritize(tokens[i]) != null && !wordList.ContainsKey(unDiacritize(tokens[i])))
                        wordList.Add(unDiacritize(tokens[i]), unDiacritize(tokens[i]));
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // SVM word list
        private static bool loadSVMWordList(String file)
        {
            try
            {
                long index = 0;
                String line = (File.OpenText(file)).ReadToEnd();
                (File.OpenText(file)).Close();

                String[] tokens = line.Split('\n', '\r');

                for (uint i = 0; i < tokens.Length; i += 2)
                {
                    if (unDiacritize(tokens[i]) != null && !svmWordList.ContainsKey(unDiacritize(tokens[i])))
                        svmWordList.Add(unDiacritize(tokens[i]), (++index) );
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // load stop words
        private static bool loadStopWords(string stopWordFile)
        {
            uint i = 0;
            String[] tok;
            String[] token;

            try
            {
                String line = (File.OpenText(stopWordFile)).ReadToEnd();
                (File.OpenText(stopWordFile)).Close();

                token = line.Split('\n', '\r');

                i = 0;
                for (i = 0; i < token.Length; i++)
                {
                    tok = token[i++].Split('\t');
                    if (tok[0] != null && !stopWord.ContainsKey(tok[0]))
                        stopWord.Add(tok[0], tok[1]);
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // load affixes
        private static bool loadAffixes(String prefixFile, String suffFile)
        {
            uint i = 0;
            String[] token;

            try
            {
                // prefix
                String line = (File.OpenText(prefixFile)).ReadToEnd();
                (File.OpenText(prefixFile)).Close();

                token = line.Split('\n', '\r');

                for (i = 0; i < token.Length; i++)
                {
                    if (unDiacritize(token[i]) != null && !prefix.ContainsKey(unDiacritize(token[i])))
                        prefix.Add(unDiacritize(token[i]), unDiacritize(token[i]));
                    i++;
                }

                // suffix
                line = (File.OpenText(suffFile)).ReadToEnd();
                (File.OpenText(suffFile)).Close();

                token = line.Split('\n', '\r');

                i = 0;
                for (i = 0; i < token.Length; i++)
                {
                    if (unDiacritize(token[i]) != null && !suffix.ContainsKey(unDiacritize(token[i])))
                        suffix.Add(unDiacritize(token[i]), unDiacritize(token[i]));
                    i++;
                }
                return true;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return false;
            }
        }

        // undiacritize word
        private static String unDiacritize(String str)
        {
            try
            {
                if (str.Length == 0 || str == "")
                    return null;

                String str_ret = null;

                for (int i = 0; i < str.Length; i++)
                    if (!(str[i] >= 1611 && str[i] <= 1618) && str[i] != 1622 && str[i] != 1623 && str[i] != 1761 && str[i] != 65279)
                        str_ret += str[i];

                return str_ret;
            }
            catch (Exception caught)
            {
                Console.WriteLine(caught.Message);
                return null;
            }
        }
    }
}