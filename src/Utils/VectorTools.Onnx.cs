using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Maenifold.Utils
{
    internal sealed class SimpleTokenizer
    {
        private readonly Dictionary<string, int> _vocab = new();
        private readonly Dictionary<int, string> _idToToken = new();

        public SimpleTokenizer()
        {
            _vocab["[PAD]"] = 0;
            _vocab["[UNK]"] = 1;
            _vocab["[CLS]"] = 2;
            _vocab["[SEP]"] = 3;
            _idToToken[0] = "[PAD]";
            _idToToken[1] = "[UNK]";
            _idToToken[2] = "[CLS]";
            _idToToken[3] = "[SEP]";
        }

        public void LoadVocab(string vocabPath)
        {
            if (!File.Exists(vocabPath))
                return;
            var lines = File.ReadAllLines(vocabPath);
            for (int i = 0; i < lines.Length; i++)
            {
                var token = lines[i].Trim();
                if (!_vocab.ContainsKey(token))
                {
                    int id = _vocab.Count;
                    _vocab[token] = id;
                    _idToToken[id] = token;
                }
            }
        }

        public EncodingResult Encode(string text, bool addSpecialTokens = true)
        {
            var words = Regex.Split(text.ToLowerInvariant(), @"\W+")
                           .Where(w => !string.IsNullOrEmpty(w))
                           .ToArray();
            var ids = new List<int>();
            var tokens = new List<string>();
            if (addSpecialTokens)
            {
                ids.Add(_vocab["[CLS]"]);
                tokens.Add("[CLS]");
            }
            foreach (var word in words)
            {
                int id = _vocab.TryGetValue(word, out var wordId) ? wordId : _vocab["[UNK]"];
                ids.Add(id);
                tokens.Add(_idToToken.TryGetValue(id, out var token) ? token : "[UNK]");
            }
            if (addSpecialTokens)
            {
                ids.Add(_vocab["[SEP]"]);
                tokens.Add("[SEP]");
            }
            return new EncodingResult { Ids = ids, Tokens = tokens };
        }
    }

    internal sealed class EncodingResult
    {
        public List<int> Ids { get; set; } = new();
        public List<string> Tokens { get; set; } = new();
    }

    public static partial class VectorTools
    {
        private static void EnsureModelLoaded()
        {
            if (!_initialized)
                LoadModel();
        }

        private static string FindAssetPath(string fileName)
        {
            var baseDir = AppContext.BaseDirectory;
            var directPath = Path.Combine(baseDir, "assets", "models", fileName);
            if (File.Exists(directPath))
                return directPath;
            var currentDir = new DirectoryInfo(baseDir);
            while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Maenifold.sln")))
                currentDir = currentDir.Parent;
            if (currentDir == null)
            {
                currentDir = new DirectoryInfo(baseDir);
                while (currentDir != null)
                {
                    var hasSln = Directory.EnumerateFiles(currentDir.FullName, "*.sln")
                        .Any(f => string.Equals(Path.GetFileName(f), "Maenifold.sln", StringComparison.OrdinalIgnoreCase));
                    if (hasSln) break;
                    currentDir = currentDir.Parent;
                }
            }
            if (currentDir == null)
                throw new DirectoryNotFoundException("Could not find repository root containing Maenifold.sln");
            return Path.Combine(currentDir.FullName, "assets", "models", fileName);
        }

        private static string FindModelPath() => FindAssetPath("all-MiniLM-L6-v2.onnx");
        private static string FindVocabPath() => FindAssetPath("vocab.txt");

        private static TensorInfo? SelectBestOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
        {
            var candidates = new List<TensorInfo>();
            foreach (var o in outputs)
            {
                try
                {
                    var t = o.AsTensor<float>();
                    candidates.Add(new TensorInfo { Name = o.Name, Tensor = t });
                    continue;
                }
                catch
                {
                }
                try
                {
                    var t2 = o.AsTensor<double>();
                    candidates.Add(new TensorInfo { Name = o.Name, TensorDouble = t2 });
                    continue;
                }
                catch
                {
                }
            }
            if (!candidates.Any())
                return null;
            var namePref = new[] { "pooled", "pooler", "pool", "sentence", "sentence_embedding", "embedding" };
            foreach (var n in namePref)
            {
                var match = candidates.FirstOrDefault(c => c.Name != null && c.Name.Contains(n, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }
            var twoD = candidates.FirstOrDefault(c => c.Dimensions.Length == 2 && c.Dimensions[0] == 1);
            if (twoD != null) return twoD;
            var oneD = candidates.FirstOrDefault(c => c.Dimensions.Length == 1);
            if (oneD != null) return oneD;
            return candidates.First();
        }

        private sealed class TensorInfo
        {
            public string? Name { get; set; }
            public Tensor<float>? Tensor { get; set; }
            public Tensor<double>? TensorDouble { get; set; }
            public int[] Dimensions
            {
                get
                {
                    if (Tensor != null) return Tensor.Dimensions.ToArray();
                    if (TensorDouble != null) return TensorDouble.Dimensions.ToArray();
                    return Array.Empty<int>();
                }
            }
        }

        private static float[] GetTensorAsFloatArray(TensorInfo info, out int[] dims)
        {
            if (info.Tensor != null)
            {
                dims = info.Tensor.Dimensions.ToArray();
                try
                {
                    return info.Tensor.ToArray();
                }
                catch
                {
                    var arr = info.Tensor.AsEnumerable().ToArray();
                    dims = info.Tensor.Dimensions.ToArray();
                    return arr;
                }
            }
            if (info.TensorDouble != null)
            {
                dims = info.TensorDouble.Dimensions.ToArray();
                var d = info.TensorDouble.ToArray();
                var f = new float[d.Length];
                for (int i = 0; i < d.Length; i++) f[i] = (float)d[i];
                return f;
            }
            dims = Array.Empty<int>();
            return Array.Empty<float>();
        }

        private static float[] PoolSequenceByAttention(float[] flattened, int seqLen, int hidden, long[] attentionMask)
        {
            var pooled = new float[hidden];
            long count = 0;
            for (int i = 0; i < seqLen; i++)
            {
                var use = 0L;
                if (i < attentionMask.Length) use = attentionMask[i];
                if (use == 0) continue;
                count++;
                var baseIndex = i * hidden;
                for (int h = 0; h < hidden; h++)
                {
                    pooled[h] += flattened[baseIndex + h];
                }
            }
            if (count == 0)
                return pooled;
            var inv = 1.0f / (float)count;
            for (int h = 0; h < hidden; h++) pooled[h] *= inv;
            return pooled;
        }

        private static float[] PadOrTruncate(float[] src, int desired)
        {
            var dst = new float[desired];
            if (src == null || src.Length == 0) return dst;
            var len = Math.Min(src.Length, desired);
            Array.Copy(src, dst, len);
            if (len < desired)
            {
                for (int i = len; i < desired; i++) dst[i] = 0f;
            }
            return dst;
        }
    }
}
