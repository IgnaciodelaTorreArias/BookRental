using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.HuggingFace.Tokenizer;

namespace Inventory.Public.Models.Sentences;
// sentence-transformers/all-MiniLM-L6-v2
public class SentencesModel
{
    readonly Tokenizer tokenizer;
    readonly InferenceSession session;
    public SentencesModel()
    {
        tokenizer = Tokenizer.FromFile("./Models/Sentences/tokenizer.json");
        session = new("./Models/Sentences/model.onnx");
    }
    (int, NamedOnnxValue[]) PrepareInputs(string text)
    {
        var encodings = tokenizer.Encode(text, true, include_type_ids: true, include_attention_mask: true).Encodings[0];
        var sequenceLenght = encodings.Ids.Count;
        var input_ids = new DenseTensor<long>(encodings.Ids.Select(t => (long)t).ToArray(), [1, sequenceLenght]);
        var type_ids = new DenseTensor<long>(encodings.TypeIds.Select(t => (long)t).ToArray(), [1, sequenceLenght]);
        var attention_mask = new DenseTensor<long>(encodings.AttentionMask.Select(t => (long)t).ToArray(), [1, sequenceLenght]);

        return (sequenceLenght, [
            NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
            NamedOnnxValue.CreateFromTensor("token_type_ids", type_ids),
            NamedOnnxValue.CreateFromTensor("attention_mask", attention_mask)
        ]);
    }
    public float[] GetEmbeddings(string text)
    {
        var (sequenceLenght, inputs) = PrepareInputs(text);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
        var outputTensor = results[0].AsEnumerable<float>().ToArray();
        float[] result = new float[384];
        for (int i = 0; i < sequenceLenght; i++)
        {
            ReadOnlySpan<float> floats = new ReadOnlySpan<float>(outputTensor, i * 384, 384);
            TensorPrimitives.Add(floats, result, result);
        }
        TensorPrimitives.Divide(result, sequenceLenght, result);
        return result;
    }
}
