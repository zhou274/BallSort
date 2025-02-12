// /*
// Created by Darsan
// */

using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private int _groupId;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Color[] _groupColors = new Color[0];
    [SerializeField] private Sprite[] _groupSprites = new Sprite[0];
    [SerializeField] private bool _useSprites = true;

    public int GroupId
    {
        get => _groupId;
        set
        {
            _groupId = value;

            if (_groupSprites.Length <= _groupId)
            {
                Debug.LogWarning("No Color Found for Id");
                return;
            }

            if (_useSprites)
                _renderer.sprite = _groupSprites[value];
            else
            {
                _renderer.color = _groupColors[value];
            }
        }
    }

    private IEnumerator MoveBallEnumerator(Vector2 target)
    {
        var startPoint = transform.position;

        yield return SimpleCoroutine.LerpNormalizedEnumerator(n => transform.position = Vector2.Lerp(startPoint, target, n),
            targetNormalized: 1.5f, lerpSpeed: 8);
        transform.position = target;
    }

    private IEnumerator MoveBallEnumerator(params Vector2[] targetPath)
    {
        foreach (var vector2 in targetPath)
        {
            yield return MoveBallEnumerator(vector2);
        }
    }

    //    public void Move(Vector2 target)
    //    {
    //        StopAllCoroutines();
    //        StartCoroutine(MoveBallEnumerator(target));
    //    }

    public void Move(params Vector2[] targetPath)
    {
        if (targetPath.Length == 0)
            return;

        StopAllCoroutines();
        StartCoroutine(MoveBallEnumerator(targetPath));
    }
}