﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.QueryParser.Flexible.Standard;
using Lucene.Net.QueryParser.Util;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Lucene.Net.QueryParser.Classic
{
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

    [TestFixture]
    public class TestQueryParser : QueryParserTestBase
    {
        public class QPTestParser : QueryParser
        {
            public QPTestParser(string f, Analyzer a)
                : base(TEST_VERSION_CURRENT, f, a)
            {
            }

            protected internal override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
            {
                throw new ParseException("Fuzzy queries not allowed");
            }

            protected internal override Query GetWildcardQuery(string field, string termStr)
            {
                throw new ParseException("Wildcard queries not allowed");
            }

        }

        // Moved to QueryParserTestBase
        //public QueryParser GetParser(Analyzer a)
        //{
        //    if (a == null) a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
        //    QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, DefaultField, a);
        //    qp.DefaultOperator = (QueryParserBase.OR_OPERATOR);
        //    return qp;
        //}

        // Moved to QueryParserTestBase
        //public override ICommonQueryParserConfiguration GetParserConfig(Analyzer a)
        //{
        //    return GetParser(a);
        //}

        // Moved to QueryParserTestBase
        //public override Query GetQuery(string query, ICommonQueryParserConfiguration cqpC)
        //{
        //    Debug.Assert(cqpC != null, "Parameter must not be null");
        //    Debug.Assert(cqpC is QueryParser, "Parameter must be instance of QueryParser");
        //    QueryParser qp = (QueryParser)cqpC;
        //    return qp.Parse(query);
        //}

        // Moved to QueryParserTestBase
        //public override Query GetQuery(string query, Analyzer a)
        //{
        //    return GetParser(a).Parse(query);
        //}

        // Moved to QueryParserTestBase
        //public override bool IsQueryParserException(Exception exception)
        //{
        //    return exception is ParseException;
        //}

        // Moved to QueryParserTestBase
        //public override void SetDefaultOperatorOR(ICommonQueryParserConfiguration cqpC)
        //{
        //    Debug.Assert(cqpC is QueryParser);
        //    QueryParser qp = (QueryParser)cqpC;
        //    qp.DefaultOperator = QueryParserBase.Operator.OR;
        //}

        // Moved to QueryParserTestBase
        //public override void SetDefaultOperatorAND(ICommonQueryParserConfiguration cqpC)
        //{
        //    Debug.Assert(cqpC is QueryParser);
        //    QueryParser qp = (QueryParser)cqpC;
        //    qp.DefaultOperator = QueryParserBase.Operator.AND;
        //}

        // Moved to QueryParserTestBase
        //public override void SetAnalyzeRangeTerms(ICommonQueryParserConfiguration cqpC, bool value)
        //{
        //    Debug.Assert(cqpC is QueryParser);
        //    QueryParser qp = (QueryParser)cqpC;
        //    qp.AnalyzeRangeTerms = (value);
        //}

        // Moved to QueryParserTestBase
        //public override void SetAutoGeneratePhraseQueries(ICommonQueryParserConfiguration cqpC, bool value)
        //{
        //    Debug.Assert(cqpC is QueryParser);
        //    QueryParser qp = (QueryParser)cqpC;
        //    qp.AutoGeneratePhraseQueries = value;
        //}

        // Moved to QueryParserTestBase
        //public override void SetDateResolution(ICommonQueryParserConfiguration cqpC, ICharSequence field, DateTools.Resolution value)
        //{
        //    Debug.Assert(cqpC is QueryParser);
        //    QueryParser qp = (QueryParser)cqpC;
        //    qp.SetDateResolution(field.toString(), value);
        //}

        [Test]
        public override void TestDefaultOperator()
        {
            QueryParser qp = GetParser(new MockAnalyzer(Random()));
            // make sure OR is the default:
            assertEquals(QueryParserBase.OR_OPERATOR, qp.DefaultOperator);
            SetDefaultOperatorAND(qp);
            assertEquals(QueryParserBase.AND_OPERATOR, qp.DefaultOperator);
            SetDefaultOperatorOR(qp);
            assertEquals(QueryParserBase.OR_OPERATOR, qp.DefaultOperator);
        }

        // LUCENE-2002: when we run javacc to regen QueryParser,
        // we also run a replaceregexp step to fix 2 of the public
        // ctors (change them to protected):
        //
        // protected QueryParser(CharStream stream)
        //
        // protected QueryParser(QueryParserTokenManager tm)
        //
        // This test is here as a safety, in case that ant step
        // doesn't work for some reason.
        [Test]
        public void TestProtectedCtors()
        {
            try
            {
                typeof(QueryParser).GetConstructor(new Type[] { typeof(ICharStream) });
                fail("please switch public QueryParser(CharStream) to be protected");
            }
            catch (Exception nsme)
            {
                // expected
            }
            try
            {
                typeof(QueryParser).GetConstructor(new Type[] { typeof(QueryParserTokenManager) });
                fail("please switch public QueryParser(QueryParserTokenManager) to be protected");
            }
            catch (Exception nsme)
            {
                // expected
            }
        }

        private class TestFuzzySlopeExtendabilityQueryParser : QueryParser
        {
            public TestFuzzySlopeExtendabilityQueryParser()
                : base(TEST_VERSION_CURRENT, "a", new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false))
            {}

            internal override Query HandleBareFuzzy(string qfield, Token fuzzySlop, string termImage)
            {
                if (fuzzySlop.image.EndsWith("€"))
                {
                    float fms = FuzzyMinSim;
                    try
                    {
                        fms = float.Parse(fuzzySlop.image.Substring(1, fuzzySlop.image.Length - 2));
                    }
                    catch (Exception ignored) { }
                    float value = float.Parse(termImage);
                    return GetRangeQuery(qfield, (value - fms / 2.0f).ToString(), (value + fms / 2.0f).ToString(), true, true);
                }
                return base.HandleBareFuzzy(qfield, fuzzySlop, termImage);
            }
        }

        [Test]
        public void TestFuzzySlopeExtendability()
        {
            QueryParser qp = new TestFuzzySlopeExtendabilityQueryParser();
            assertEquals(qp.Parse("a:[11.95 TO 12.95]"), qp.Parse("12.45~1€"));
        }

        private class TestStarParsingQueryParser : QueryParser
        {
            public readonly int[] type = new int[1];

            public TestStarParsingQueryParser()
                : base(TEST_VERSION_CURRENT, "field", new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false))
            { }

            protected internal override Query GetWildcardQuery(string field, string termStr)
            {
                // override error checking of superclass
                type[0] = 1;
                return new TermQuery(new Index.Term(field, termStr));
            }

            protected internal override Query GetPrefixQuery(string field, string termStr)
            {
                // override error checking of superclass
                type[0] = 2;
                return new TermQuery(new Index.Term(field, termStr));
            }

            protected internal override Query GetFieldQuery(string field, string queryText, bool quoted)
            {
                type[0] = 3;
                return base.GetFieldQuery(field, queryText, quoted);
            }
        }

        [Test]
        public override void TestStarParsing()
        {
            TestStarParsingQueryParser qp = new TestStarParsingQueryParser();

            TermQuery tq;

            tq = (TermQuery)qp.Parse("foo:zoo*");
            assertEquals("zoo", tq.Term.Text());
            assertEquals(2, qp.type[0]);

            tq = (TermQuery)qp.Parse("foo:zoo*^2");
            assertEquals("zoo", tq.Term.Text());
            assertEquals(2, qp.type[0]);
            assertEquals(tq.Boost, 2, 0);

            tq = (TermQuery)qp.Parse("foo:*");
            assertEquals("*", tq.Term.Text());
            assertEquals(1, qp.type[0]); // could be a valid prefix query in the future too

            tq = (TermQuery)qp.Parse("foo:*^2");
            assertEquals("*", tq.Term.Text());
            assertEquals(1, qp.type[0]);
            assertEquals(tq.Boost, 2, 0);

            tq = (TermQuery)qp.Parse("*:foo");
            assertEquals("*", tq.Term.Field);
            assertEquals("foo", tq.Term.Text());
            assertEquals(3, qp.type[0]);

            tq = (TermQuery)qp.Parse("*:*");
            assertEquals("*", tq.Term.Field);
            assertEquals("*", tq.Term.Text());
            assertEquals(1, qp.type[0]); // could be handled as a prefix query in the
            // future

            tq = (TermQuery)qp.Parse("(*:*)");
            assertEquals("*", tq.Term.Field);
            assertEquals("*", tq.Term.Text());
            assertEquals(1, qp.type[0]);
        }

        [Test]
        public void TestCustomQueryParserWildcard()
        {
            try
            {
                new QPTestParser("contents", new MockAnalyzer(Random(),
                    MockTokenizer.WHITESPACE, false)).Parse("a?t");
                fail("Wildcard queries should not be allowed");
            }
            catch (ParseException expected)
            {
                // expected exception
            }
        }

        [Test]
        public void TestCustomQueryParserFuzzy()
        {
            try
            {
                new QPTestParser("contents", new MockAnalyzer(Random(),
                    MockTokenizer.WHITESPACE, false)).Parse("xunit~");
                fail("Fuzzy queries should not be allowed");
            }
            catch (ParseException expected)
            {
                // expected exception
            }
        }

        /// <summary>
        /// query parser that doesn't expand synonyms when users use double quotes
        /// </summary>
        private class SmartQueryParser : QueryParser
        {
            Analyzer morePrecise = new Analyzer2();

            public SmartQueryParser()
                : base(TEST_VERSION_CURRENT, "field", new Analyzer1())
            {
            }

            protected internal override Query GetFieldQuery(string field, string queryText, bool quoted)
            {
                if (quoted) return NewFieldQuery(morePrecise, field, queryText, quoted);
                else return base.GetFieldQuery(field, queryText, quoted);
            }
        }

        public override void TestNewFieldQuery()
        {
            /** ordinary behavior, synonyms form uncoordinated boolean query */
            QueryParser dumb = new QueryParser(TEST_VERSION_CURRENT, "field",
                new Analyzer1());
            BooleanQuery expanded = new BooleanQuery(true);
            expanded.Add(new TermQuery(new Index.Term("field", "dogs")),
                BooleanClause.Occur.SHOULD);
            expanded.Add(new TermQuery(new Index.Term("field", "dog")),
                BooleanClause.Occur.SHOULD);
            assertEquals(expanded, dumb.Parse("\"dogs\""));
            /** even with the phrase operator the behavior is the same */
            assertEquals(expanded, dumb.Parse("dogs"));

            /**
             * custom behavior, the synonyms are expanded, unless you use quote operator
             */
            QueryParser smart = new SmartQueryParser();
            assertEquals(expanded, smart.Parse("dogs"));

            Query unexpanded = new TermQuery(new Index.Term("field", "dogs"));
            assertEquals(unexpanded, smart.Parse("\"dogs\""));
        }

        // LUCENETODO: fold these into QueryParserTestBase

        /// <summary>
        /// adds synonym of "dog" for "dogs".
        /// </summary>
        public class MockSynonymAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new MockSynonymFilter(tokenizer));
            }
        }

        /// <summary>
        /// simple synonyms test
        /// </summary>
        [Test]
        public void TestSynonyms()
        {
            BooleanQuery expected = new BooleanQuery(true);
            expected.Add(new TermQuery(new Index.Term("field", "dogs")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Index.Term("field", "dog")), BooleanClause.Occur.SHOULD);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockSynonymAnalyzer());
            assertEquals(expected, qp.Parse("dogs"));
            assertEquals(expected, qp.Parse("\"dogs\""));
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("dogs"));
            assertEquals(expected, qp.Parse("\"dogs\""));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("dogs^2"));
            assertEquals(expected, qp.Parse("\"dogs\"^2"));
        }

        /// <summary>
        /// forms multiphrase query
        /// </summary>
        [Test]
        public void TestSynonymsPhrase()
        {
            MultiPhraseQuery expected = new MultiPhraseQuery();
            expected.Add(new Index.Term("field", "old"));
            expected.Add(new Index.Term[] { new Index.Term("field", "dogs"), new Index.Term("field", "dog") });
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockSynonymAnalyzer());
            assertEquals(expected, qp.Parse("\"old dogs\""));
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("\"old dogs\""));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("\"old dogs\"^2"));
            expected.Slop = (3);
            assertEquals(expected, qp.Parse("\"old dogs\"~3^2"));
        }

        /// <summary>
        /// adds synonym of "國" for "国".
        /// </summary>
        protected internal class MockCJKSynonymFilter : TokenFilter
        {
            internal ICharTermAttribute TermAtt;
            internal IPositionIncrementAttribute PosIncAtt;
            internal bool AddSynonym = false;

            public MockCJKSynonymFilter(TokenStream input)
                : base(input)
            {
                TermAtt = AddAttribute<ICharTermAttribute>();
                PosIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (AddSynonym) // inject our synonym
                {
                    ClearAttributes();
                    TermAtt.SetEmpty().Append("國");
                    PosIncAtt.PositionIncrement = 0;
                    AddSynonym = false;
                    return true;
                }

                if (input.IncrementToken())
                {
                    AddSynonym = TermAtt.ToString().Equals("国");
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        protected class MockCJKSynonymAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer tokenizer = new SimpleCJKTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new MockCJKSynonymFilter(tokenizer));
            }
        }

        /// <summary>
        /// simple CJK synonym test
        /// </summary>
        [Test]
        public void TestCJKSynonym()
        {
            BooleanQuery expected = new BooleanQuery(true);
            expected.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            assertEquals(expected, qp.Parse("国"));
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("国"));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("国^2"));
        }

        /// <summary>
        /// synonyms with default OR operator 
        /// </summary>
        [Test]
        public void TestCJKSynonymsOR()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Index.Term("field", "中")), BooleanClause.Occur.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.SHOULD);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            assertEquals(expected, qp.Parse("中国"));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("中国^2"));
        }

        /// <summary>
        /// more complex synonyms with default OR operator
        /// </summary>
        [Test]
        public void TestCJKSynonymsOR2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Index.Term("field", "中")), BooleanClause.Occur.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.SHOULD);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner2.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner2, BooleanClause.Occur.SHOULD);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            assertEquals(expected, qp.Parse("中国国"));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("中国国^2"));
        }

        /// <summary>
        /// synonyms with default AND operator
        /// </summary>
        [Test]
        public void TestCJKSynonymsAND()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Index.Term("field", "中")), BooleanClause.Occur.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.MUST);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("中国"));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("中国^2"));
        }

        /// <summary>
        /// more complex synonyms with default AND operator
        /// </summary>
        [Test]
        public void TestCJKSynonymsAND2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Index.Term("field", "中")), BooleanClause.Occur.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.MUST);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Index.Term("field", "国")), BooleanClause.Occur.SHOULD);
            inner2.Add(new TermQuery(new Index.Term("field", "國")), BooleanClause.Occur.SHOULD);
            expected.Add(inner2, BooleanClause.Occur.MUST);
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("中国国"));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("中国国^2"));
        }

        [Test]
        public void TestCJKSynonymsPhrase()
        {
            MultiPhraseQuery expected = new MultiPhraseQuery();
            expected.Add(new Index.Term("field", "中"));
            expected.Add(new Index.Term[] { new Index.Term("field", "国"), new Index.Term("field", "國") });
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new MockCJKSynonymAnalyzer());
            qp.DefaultOperator = (QueryParserBase.Operator.AND);
            assertEquals(expected, qp.Parse("\"中国\""));
            expected.Boost = (2.0f);
            assertEquals(expected, qp.Parse("\"中国\"^2"));
            expected.Slop = (3);
            assertEquals(expected, qp.Parse("\"中国\"~3^2"));
        }

    }
}
