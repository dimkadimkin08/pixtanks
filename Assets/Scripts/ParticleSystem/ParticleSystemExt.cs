using UnityEngine;
public static class ParticleSystemExt
{
    public static void SetStartColorKeepAlpha(this ParticleSystem particleSystem, Color color)
    {
        var main = particleSystem.main;
        var startColor = main.startColor;
        switch (startColor.mode)
        {
            case ParticleSystemGradientMode.Color:
                startColor.color = new Color(color.r, color.g, color.b, startColor.color.a);
                break;
            case ParticleSystemGradientMode.RandomColor:
            case ParticleSystemGradientMode.Gradient:
                var gradient = startColor.gradient;
                for (var i = 0; i < gradient.colorKeys.Length; i++)
                    gradient.colorKeys[i].color = color;
                break;
            case ParticleSystemGradientMode.TwoColors:
                startColor.colorMin = new Color(color.r, color.g, color.b, startColor.colorMin.a);
                startColor.colorMax = new Color(color.r, color.g, color.b, startColor.colorMax.a);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                var gradientMin = startColor.gradientMin;
                for (var i = 0; i < gradientMin.colorKeys.Length; i++)
                    gradientMin.colorKeys[i].color = color;
                var gradientMax = startColor.gradientMax;
                for (var i = 0; i < gradientMax.colorKeys.Length; i++)
                    gradientMax.colorKeys[i].color = color;
                break;
        }
        main.startColor = startColor;
        if (particleSystem.isPlaying)
        {
            particleSystem.Stop();
            particleSystem.Clear();
            particleSystem.Play();
        }
    }
}
