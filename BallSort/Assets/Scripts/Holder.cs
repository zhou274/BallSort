using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Holder : MonoBehaviour,IInitializable<IEnumerable<Ball>>
{
    [SerializeField] private int _maxBalls;
    [SerializeField] private float _ballRadius;

    [SerializeField] private AudioClip _popClip,_putClip;

    private readonly List<Ball> _balls = new List<Ball>();
    private bool _isPending;

    public bool IsFull => _balls.Count >= _maxBalls;
    public Ball TopBall => _balls.LastOrDefault();
    public IEnumerable<Ball> Balls => _balls;

    public bool IsPending
    {
        get => _isPending;
        set
        {
            if(_isPending==value)
                return;
            if(_balls.Count==0)
            {
                _isPending = false;
                return;
            }

            if (value)
            {
                PendingBall = _balls.Last();
                PendingBall.Move(PendingPoint);
                PlayClipIfCan(_popClip);
            }
            else if(PendingBall != null)
            {
                PendingBall.Move(GetBallPosition(_balls.Count - 1));
                PendingBall = null;
                PlayClipIfCan(_popClip);
            }

            _isPending = value;
        }
    }
    public bool Initialized { get; private set; }
    public Ball PendingBall { get; private set; }
    public Vector2 PendingPoint => GetBallPosition(_maxBalls) + 0.5f*Vector2.up;

    public Ball RemoveTopBall()
    {
        if(_balls.Count == 0)
            throw new InvalidOperationException();

        var ball = _balls.Last();
        _balls.Remove(ball);
        PendingBall = null;
        IsPending = false;
        return ball;
    }

    public void Move(Ball ball)
    {
        if (IsPending)
        {
            IsPending = false;
        }
        PlayClipIfCan(_putClip);
        _balls.Add(ball);
        ball.Move(PendingPoint+Vector2.up*0.1f,GetBallPosition(_balls.Count-1));
    }


    public Vector2 GetBallPosition(int index)
    {
        return transform.TransformPoint((index + 0.5f) * 2 * _ballRadius * Vector2.up);
    }

    private void PlayClipIfCan(AudioClip clip,float volume=0.35f)
    {
        if(!AudioManager.IsSoundEnable || clip==null)
            return;
        AudioSource.PlayClipAtPoint(clip,Camera.main.transform.position,volume);
    }

    public void Init(IEnumerable<Ball> balls)
    {
        var list = balls.ToList();
        if(Initialized)
            return;
        _balls.AddRange(list);
        for (var i = 0; i < _balls.Count; i++)
        {
            _balls[i].transform.position = GetBallPosition(i);
        }
        Initialized = true;
    }
}