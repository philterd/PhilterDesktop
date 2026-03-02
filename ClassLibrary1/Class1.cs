using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassLibrary1
{
    public class GlinerModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _modelPath;
        private bool _disposed = false;

        public GlinerModel(string modelPath)
        {
            _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
            
            var sessionOptions = new SessionOptions();
            sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            
            _session = new InferenceSession(modelPath, sessionOptions);
        }

        /// <summary>
        /// Performs named entity recognition on the input text
        /// </summary>
        /// <param name="text">Input text to analyze</param>
        /// <param name="labels">Entity labels to recognize (e.g., "person", "organization", "location")</param>
        /// <param name="threshold">Confidence threshold for entity detection (default: 0.5)</param>
        /// <returns>List of detected entities with their positions and scores</returns>
        public List<Entity> PredictEntities(string text, string[] labels, float threshold = 0.5f)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            
            if (labels == null || labels.Length == 0)
                throw new ArgumentException("Labels cannot be null or empty", nameof(labels));

            System.Diagnostics.Debug.WriteLine($"Predicting entities in text: '{text}' with labels: [{string.Join(", ", labels)}] and threshold: {threshold}");

            // Tokenize input
            var tokens = SimpleTokenize(text);
            var inputIds = ConvertTokensToIds(tokens);

            // Prepare label embeddings
            var labelTokens = labels.SelectMany(l => SimpleTokenize(l)).ToList();
            var labelIds = ConvertTokensToIds(labelTokens);

            // Generate all seq_len × maxSpanWidth span slots required by the model's Reshape node.
            // The model always expects inputIds.Count * maxSpanWidth spans; span_mask marks invalid ones.
            const int maxSpanWidth = 12; // Must match the model's compiled architecture
            var spans = new List<(int start, int end)>();
            var spanMasks = new List<bool>();

            for (int s = 0; s < inputIds.Count; s++)
            {
                for (int w = 1; w <= maxSpanWidth; w++)
                {
                    int end = s + w;
                    // Valid: s is a real word token (not [CLS] at 0 or [SEP] at last)
                    // and the span stays within the word region
                    bool valid = s >= 1 && end <= tokens.Count;
                    spans.Add((s, Math.Min(end, inputIds.Count - 1)));
                    spanMasks.Add(valid);
                }
            }
            // spans.Count == inputIds.Count * maxSpanWidth (e.g., 9 * 12 = 108)

            // Create span_idx tensor [batch_size, num_spans, 2]
            var spanIdxData = new long[spans.Count * 2];
            for (int i = 0; i < spans.Count; i++)
            {
                spanIdxData[i * 2] = spans[i].start;
                spanIdxData[i * 2 + 1] = spans[i].end;
            }
            var spanIdxTensor = new DenseTensor<long>(spanIdxData, new[] { 1, spans.Count, 2 });

            // Create span_mask tensor [batch_size, num_spans] — model metadata requires Bool
            var spanMaskTensor = new DenseTensor<bool>(
                spanMasks.ToArray(),
                new[] { 1, spans.Count });

            // Create input tensors
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", 
                    new DenseTensor<long>(inputIds.ToArray(), new[] { 1, inputIds.Count })),
                NamedOnnxValue.CreateFromTensor("attention_mask",
                    new DenseTensor<long>(Enumerable.Repeat(1L, inputIds.Count).ToArray(), new[] { 1, inputIds.Count })),
                NamedOnnxValue.CreateFromTensor("words_mask",
                    new DenseTensor<long>(Enumerable.Repeat(1L, inputIds.Count).ToArray(), new[] { 1, inputIds.Count })),
                NamedOnnxValue.CreateFromTensor("text_lengths",
                    new DenseTensor<long>(new[] { (long)inputIds.Count }, new[] { 1, 1 })),
                NamedOnnxValue.CreateFromTensor("span_idx", spanIdxTensor),
                NamedOnnxValue.CreateFromTensor("span_mask", spanMaskTensor)
            };

            // Run inference
            using var results = _session.Run(inputs);
            
            // Process outputs
            var entities = new List<Entity>();
            var output = results.First().AsEnumerable<float>().ToArray();
            
            // Parse entities from model output
            entities = ParseEntities(text, tokens, output, spans, spanMasks, labels, threshold);

            return entities;
        }

        /// <summary>
        /// Simple whitespace tokenization
        /// </summary>
        private List<string> SimpleTokenize(string text)
        {
            var tokens = new List<string>();
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                tokens.Add(word);
            }
            
            return tokens;
        }

        /// <summary>
        /// Convert tokens to IDs (simplified - in production use proper tokenizer like BPE)
        /// </summary>
        private List<long> ConvertTokensToIds(List<string> tokens)
        {
            var ids = new List<long> { 101 }; // [CLS] token
            
            foreach (var token in tokens)
            {
                // Simple hash-based ID generation (replace with proper vocab mapping)
                var id = Math.Abs(token.GetHashCode() % 30000) + 1000;
                ids.Add(id);
            }
            
            ids.Add(102); // [SEP] token
            return ids;
        }

        /// <summary>
        /// Parse entities from model output tensor shaped [1, num_spans, num_labels].
        /// Each span already represents a multi-token entity, so no merging is needed.
        /// </summary>
        private List<Entity> ParseEntities(string text, List<string> tokens, float[] scores,
            List<(int start, int end)> spans, List<bool> spanMasks, string[] labels, float threshold)
        {
            var entities = new List<Entity>();
            int numLabels = labels.Length;

            // Pre-compute character offsets for each token in the original text
            var tokenCharStarts = new int[tokens.Count];
            var tokenCharEnds = new int[tokens.Count];
            int charPos = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                int idx = text.IndexOf(tokens[i], charPos);
                tokenCharStarts[i] = idx >= 0 ? idx : charPos;
                tokenCharEnds[i] = tokenCharStarts[i] + tokens[i].Length;
                charPos = tokenCharEnds[i];
            }

            // Output tensor layout: [1, num_spans, num_labels] flattened row-major
            // span_idx uses inputIds-based positions: [CLS]=0, words=1..tokens.Count, [SEP]=last
            for (int spanIdx = 0; spanIdx < spans.Count; spanIdx++)
            {
                if (!spanMasks[spanIdx]) continue;

                var (startPos, endPos) = spans[spanIdx];
                // Shift from inputIds positions to tokens[] indices ([CLS] occupies inputIds[0])
                int tokenStart = startPos - 1;  // inclusive
                int tokenEnd   = endPos   - 1;  // exclusive

                if (tokenStart < 0 || tokenEnd <= tokenStart || tokenEnd > tokens.Count) continue;

                for (int labelIdx = 0; labelIdx < numLabels; labelIdx++)
                {
                    int scoreIdx = spanIdx * numLabels + labelIdx;
                    if (scoreIdx >= scores.Length) continue;

                    float score = scores[scoreIdx];
                    if (score > threshold)
                    {
                        int charStart = tokenCharStarts[tokenStart];
                        int charEnd   = tokenCharEnds[tokenEnd - 1];
                        entities.Add(new Entity
                        {
                            Text  = text.Substring(charStart, charEnd - charStart),
                            Label = labels[labelIdx],
                            Start = charStart,
                            End   = charEnd,
                            Score = score
                        });
                    }
                }
            }

            return entities;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a named entity detected by GLiNER
    /// </summary>
    public class Entity
    {
        public string Text { get; set; }
        public string Label { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public float Score { get; set; }

        public override string ToString()
        {
            return $"{Label}: '{Text}' ({Score:F3}) [{Start}:{End}]";
        }
    }
}
