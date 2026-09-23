using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
[RequireComponent(typeof(InputField))]
public class InputFieldIntegerRangeFilter : MonoBehaviour
{
    [SerializeField]
    private int minValue;
    [SerializeField]
    private int maxValue;

    public UnityEvent<int> onSubmit;
    public UnityEvent<int> onEndEdit;

    public int MinValue
    {
        get => minValue;
        set => minValue = value;
    }

    public int MaxValue
    {
        get => maxValue;
        set => maxValue = value;
    }

    private InputField _inputField;

    private void Start()
    {
        _inputField = GetComponent<InputField>();
        _inputField.onSubmit.AddListener(OnSendInput);
        _inputField.onEndEdit.AddListener(OnSendInput);
    }

    private void OnSendInput(string text)
    {
        if (text.TryParseNoLocale(out ushort number))
        {
            if (number < minValue)
            {
                _inputField.text = minValue.ToString();
                onSubmit?.Invoke(minValue);
            }
            else if (number > maxValue)
            {
                _inputField.text = maxValue.ToString();
                onSubmit?.Invoke(maxValue);
            }
            else
            {
                onSubmit?.Invoke(number);
            }
        }
        else
        {
            _inputField.text = minValue.ToString();
            onSubmit?.Invoke(minValue);
        }
    }
}
