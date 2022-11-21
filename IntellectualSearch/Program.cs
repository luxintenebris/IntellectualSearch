using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;
using System.IO;
using System.Linq;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Analysis.En;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spell;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace IntellectualSearch
{
    class Program
    {
        // Specify the compatibility version we want
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        static IndexWriter writer;
        static Analyzer analyzer;

        static void CreateExampleIndex(LuceneDirectory indexDir)
        {
            //Create an analyzer to process the text 
            analyzer = new EnglishAnalyzer(luceneVersion);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
            indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
            writer = new IndexWriter(indexDir, indexConfig);

            //Add three documents to the index
            Document doc = new Document();
            doc.Add(new TextField("title", "The Apache Software Foundation - The world's largest open source foundation.", Field.Store.YES));
            doc.Add(new StringField("domain", "www.apache.org", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("title", "Powerful open source search library for .NET", Field.Store.YES));
            doc.Add(new StringField("domain", "lucenenet.apache.org", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("title", "Unique gifts made by small businesses in North Carolina.", Field.Store.YES));
            doc.Add(new StringField("domain", "www.giftoasis.com", Field.Store.YES));
            writer.AddDocument(doc);

            //Flush and commit the index data to the directory
            writer.Commit();
        }

        static void CreateNewsTextIndex(LuceneDirectory indexDir, string sourceFolderPath, int filesCountLimit = int.MaxValue)
        {
            //Create an analyzer to process the text 
            analyzer = new RussianAnalyzer(luceneVersion);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
            indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
            writer = new IndexWriter(indexDir, indexConfig);
            var filesIndexed = 0;
            foreach (string file in System.IO.Directory.EnumerateFiles(sourceFolderPath, "*.txt"))
            {
                try
                {
                    Document doc = new Document();
                    string[] contents = File.ReadAllLines(file, System.Text.Encoding.GetEncoding("windows-1251"));
                    doc.Add(new StoredField("filename", file));
                    doc.Add(new TextField("title", contents[0], Field.Store.YES));
                    doc.Add(new TextField("content", contents[1], Field.Store.YES));
                    writer.AddDocument(doc);
                    filesIndexed++;
                }
                catch (IndexOutOfRangeException) { }
                if (filesIndexed >= filesCountLimit)
                {
                    break;
                }
            }

            //Flush and commit the index data to the directory
            writer.Commit();
        }

        static string[] GetSuggestions(string searchText, DirectoryReader reader)
        {
            SpellChecker spellChecker = new SpellChecker(new RAMDirectory());
            IndexWriterConfig config = new IndexWriterConfig(luceneVersion, analyzer);
            spellChecker.IndexDictionary(new LuceneDictionary(reader, "content"), config, fullMerge: false);

            string[] suggestions = spellChecker.SuggestSimilar(searchText, 5);
            return suggestions;
        }

        static void Search(string searchText, string[] searchFields, string returnField, int maxSearchItems)
        {
            searchText = searchText.ToLower();
            using (DirectoryReader reader = writer.GetReader(applyAllDeletes: true))
            {
                string[] suggestions = GetSuggestions(searchText, reader);

                IndexSearcher searcher = new IndexSearcher(reader);
                QueryParser parser = new MultiFieldQueryParser(luceneVersion, searchFields, analyzer);

                var searchTerms = new string[] { searchText }.Union(suggestions);
                Console.WriteLine("Searching: " + string.Join(", ", searchTerms));
                IEnumerable<ScoreDoc> foundDocs = new ScoreDoc[] { }; 
                
                foreach (var searchTerm in searchTerms)
                {
                    Query query = parser.Parse(searchTerm);
                    TopDocs topDocs = searcher.Search(query, n: maxSearchItems);         //indicate we want the first 3 results

                    Console.WriteLine($"Matching results: {topDocs.TotalHits}");
                    foundDocs = topDocs.ScoreDocs.Take(maxSearchItems).Union(foundDocs);
                }
                foundDocs = foundDocs.OrderByDescending(x => x.Score).Take(maxSearchItems).ToArray();
                
               // var i = 0;
                List<string> resD = new List<string>();

                foreach (var doc in foundDocs)
                {
                    //read back a doc from results
                    Document resultDoc = searcher.Doc(doc.Doc);
                    string domainFull = resultDoc.Get(returnField);
                    string domain = Helper.SplitAndReturnLastWord(domainFull, @"//");
                    resD.Add(domain);
                    /*Console.WriteLine($"Domain of result {i + 1}: {domain} {doc.Score}");
                    i++;*/
                }
                resD = resD.Distinct().ToList();
          
                for (int i = 0; i < resD.Count && i < 10; i++)
                {
                    Console.WriteLine($"Domain of result {i + 1}: {resD[i]}");
                }
            }
        }

        static void CreateAndSearchTest(string toSearch)
        {
            //Open the Directory using a Lucene Directory class
            string indexName = "example_index";
            string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

            using (LuceneDirectory indexDir = FSDirectory.Open(indexPath))
            {
                CreateExampleIndex(indexDir);

                Search(toSearch, new string[] { "title" }, "domain", maxSearchItems: 10);
            }
        }

        static void CreateAndSearchRu(string toSearch)
        {
            //Open the Directory using a Lucene Directory class
            string indexName = "news_index";
            string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

            using (LuceneDirectory indexDir = FSDirectory.Open(indexPath))
            {
                CreateNewsTextIndex(indexDir, "C://Users//Nami//OneDrive//Документы//ИнтПоиск//Archive", filesCountLimit: 6676);

                Search(toSearch, new string[] { "title", "content" }, "filename", maxSearchItems: 6679);
            }
        }

        static void Main(string[] args)
        {
            CreateAndSearchRu("боскетбол");
            Console.ReadKey();
        }
    }
}
