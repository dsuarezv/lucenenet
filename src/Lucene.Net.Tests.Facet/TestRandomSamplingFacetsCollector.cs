﻿using System;
using System.Diagnostics;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;

namespace Lucene.Net.Facet
{

    using Document = Lucene.Net.Documents.Document;
    using Store = Lucene.Net.Documents.Field.Store;
    using StringField = Lucene.Net.Documents.StringField;
    using MatchingDocs = Lucene.Net.Facet.FacetsCollector.MatchingDocs;
    using FastTaxonomyFacetCounts = Lucene.Net.Facet.Taxonomy.FastTaxonomyFacetCounts;
    using TaxonomyReader = Lucene.Net.Facet.Taxonomy.TaxonomyReader;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MultiCollector = Lucene.Net.Search.MultiCollector;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    public class TestRandomSamplingFacetsCollector : FacetTestCase
    {

        [Test]
        public virtual void TestRandomSampling()
        {
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            FacetsConfig config = new FacetsConfig();

            int numDocs = AtLeast(10000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("EvenOdd", (i % 2 == 0) ? "even" : "odd", Store.NO));
                doc.Add(new FacetField("iMod10", Convert.ToString(i % 10)));
                writer.AddDocument(config.Build(taxoWriter, doc));
            }
            Random random = Random();

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.Reader);
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            IOUtils.Close(writer, taxoWriter);

            // Test empty results
            RandomSamplingFacetsCollector collectRandomZeroResults = new RandomSamplingFacetsCollector(numDocs / 10, random.NextLong());

            // There should be no divisions by zero
            searcher.Search(new TermQuery(new Term("EvenOdd", "NeverMatches")), collectRandomZeroResults);

            // There should be no divisions by zero and no null result
            Assert.NotNull(collectRandomZeroResults.GetMatchingDocs);

            // There should be no results at all
            foreach (MatchingDocs doc in collectRandomZeroResults.GetMatchingDocs)
            {
                Assert.AreEqual(0, doc.totalHits);
            }

            // Now start searching and retrieve results.

            // Use a query to select half of the documents.
            TermQuery query = new TermQuery(new Term("EvenOdd", "even"));

            // there will be 5 facet values (0, 2, 4, 6 and 8), as only the even (i %
            // 10) are hits.
            // there is a REAL small chance that one of the 5 values will be missed when
            // sampling.
            // but is that 0.8 (chance not to take a value) ^ 2000 * 5 (any can be
            // missing) ~ 10^-193
            // so that is probably not going to happen.
            int maxNumChildren = 5;

            RandomSamplingFacetsCollector random100Percent = new RandomSamplingFacetsCollector(numDocs, random.NextLong()); // no sampling
            RandomSamplingFacetsCollector random10Percent = new RandomSamplingFacetsCollector(numDocs / 10, random.NextLong()); // 10 % of total docs, 20% of the hits

            FacetsCollector fc = new FacetsCollector();

            searcher.Search(query, MultiCollector.Wrap(fc, random100Percent, random10Percent));

            FastTaxonomyFacetCounts random10FacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, random10Percent);
            FastTaxonomyFacetCounts random100FacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, random100Percent);
            FastTaxonomyFacetCounts exactFacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, fc);

            FacetResult random10Result = random10Percent.AmortizeFacetCounts(random10FacetCounts.GetTopChildren(10, "iMod10"), config, searcher);
            FacetResult random100Result = random100FacetCounts.GetTopChildren(10, "iMod10");
            FacetResult exactResult = exactFacetCounts.GetTopChildren(10, "iMod10");

            Assert.AreEqual(random100Result, exactResult);

            // we should have five children, but there is a small chance we have less.
            // (see above).
            Assert.True(random10Result.ChildCount <= maxNumChildren);
            // there should be one child at least.
            Assert.True(random10Result.ChildCount >= 1);

            // now calculate some statistics to determine if the sampled result is 'ok'.
            // because random sampling is used, the results will vary each time.
            int sum = 0;
            foreach (LabelAndValue lav in random10Result.LabelValues)
            {
                sum += (int)lav.value;
            }
            float mu = (float)sum / (float)maxNumChildren;

            float variance = 0;
            foreach (LabelAndValue lav in random10Result.LabelValues)
            {
                variance += (float)Math.Pow((mu - (int)lav.value), 2);
            }
            variance = variance / maxNumChildren;
            float sigma = (float)Math.Sqrt(variance);

            // we query only half the documents and have 5 categories. The average
            // number of docs in a category will thus be the total divided by 5*2
            float targetMu = numDocs / (5.0f * 2.0f);

            // the average should be in the range and the standard deviation should not
            // be too great
            Assert.True(sigma < 200);
            Assert.True(targetMu - 3 * sigma < mu && mu < targetMu + 3 * sigma);

            IOUtils.Close(searcher.IndexReader, taxoReader, dir, taxoDir);
        }

    }

}