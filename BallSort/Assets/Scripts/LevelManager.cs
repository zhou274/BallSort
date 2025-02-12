// /*
// Created by Darsan
// */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyGame;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public static event Action LevelCompleted;

    [SerializeField] private float _minXDistanceBetweenHolders;
    [SerializeField] private Camera _camera;
    [SerializeField] private Holder _holderPrefab;
    [SerializeField] private Ball _ballPrefab;

    [SerializeField] private AudioClip _winClip;

    public GameMode GameMode { get; private set; } = GameMode.Easy;
    public Level Level { get; private set; }

    private readonly List<Holder> _holders = new List<Holder>();

    private readonly Stack<MoveData> _undoStack = new Stack<MoveData>();

    public State CurrentState { get; private set; } = State.None;

    public bool HaveUndo => _undoStack.Count > 0;

    private void Awake()
    {
        Instance = this;
        var loadGameData = GameManager.LoadGameData;
        GameMode = loadGameData.GameMode;
        Level = loadGameData.Level;
        LoadLevel();
        CurrentState = State.Playing;
    }

    private void LoadLevel()
    {
        var list = PositionsForHolders(Level.map.Count, out var width).ToList();
        _camera.orthographicSize = 0.5f * width * Screen.height / Screen.width;

        for (var i = 0; i < Level.map.Count; i++)
        {
            var levelColumn = Level.map[i];
            var holder = Instantiate(_holderPrefab, list[i], Quaternion.identity);

            holder.Init(levelColumn.Select(g =>
            {
                var ball = Instantiate(_ballPrefab);
                ball.GroupId = g;
                return ball;
            }));

            _holders.Add(holder);
        }
    }

    public void OnClickUndo()
    {
        if(CurrentState!=State.Playing || _undoStack.Count<=0)
            return;

        var moveData = _undoStack.Pop();
        MoveBallFromOneToAnother(moveData.ToHolder,moveData.FromHolder);
    }

    private void Update()
    {

        if(CurrentState != State.Playing)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            var collider = Physics2D.OverlapPoint(_camera.ScreenToWorldPoint(Input.mousePosition));
            if (collider != null)
            {
                var holder = collider.GetComponent<Holder>();

                if (holder != null)
                    OnClickHolder(holder);
            }
        }
    }

    private void OnClickHolder(Holder holder)
    {
        var pendingHolder = _holders.FirstOrDefault(h => h.IsPending);

        if (pendingHolder != null && pendingHolder != holder)
        {
            if (holder.TopBall == null || (pendingHolder.TopBall.GroupId == holder.TopBall.GroupId && !holder.IsFull))
            {
                _undoStack.Push(new MoveData
                {
                    FromHolder = pendingHolder,
                    ToHolder = holder,
                    Ball = pendingHolder.TopBall
                });
                MoveBallFromOneToAnother(pendingHolder,holder);

            }
            else
            {
                pendingHolder.IsPending = false;
                holder.IsPending = true;
            }
        }
        else
        {
            if (holder.Balls.Any())
                holder.IsPending = !holder.IsPending;
        }
    }

    private void MoveBallFromOneToAnother(Holder fromHolder,Holder toHolder)
    {
        toHolder.Move(fromHolder.RemoveTopBall());
        CheckAndGameOver();
    }

    private void CheckAndGameOver()
    {
        if (_holders.All(holder =>
        {
            var balls = holder.Balls.ToList();
            return balls.Count == 0 || balls.All(ball => ball.GroupId == balls.First().GroupId);
        }) && _holders.Where(holder => holder.Balls.Any()).GroupBy(holder => holder.Balls.First().GroupId).All(holders => holders.Count()==1)) 
        {
            OverTheGame();
        }
    }

    private void OverTheGame()
    {
        if(CurrentState!=State.Playing)
            return;

        PlayClipIfCan(_winClip);
        CurrentState = State.Over;
      
        ResourceManager.CompleteLevel(GameMode,Level.no);
        LevelCompleted?.Invoke();
    }

    private void PlayClipIfCan(AudioClip clip,float volume=0.35f)
    {
        if(!AudioManager.IsSoundEnable || clip ==null)
            return;
        AudioSource.PlayClipAtPoint(clip,Camera.main.transform.position,volume);
    }

    public IEnumerable<Vector2> PositionsForHolders(int count, out float expectWidth)
    {
        expectWidth = 4 * _minXDistanceBetweenHolders;
        if (count <= 6)
        {
            var minPoint = transform.position - ((count - 1) / 2f) * _minXDistanceBetweenHolders * Vector3.right - Vector3.up*1f;

            expectWidth = Mathf.Max(count * _minXDistanceBetweenHolders, expectWidth);

            return Enumerable.Range(0, count)
                .Select(i => (Vector2) minPoint + i * _minXDistanceBetweenHolders * Vector2.right);
        }

        var aspect = (float) Screen.width / Screen.height;

        var maxCountInRow = Mathf.CeilToInt(count / 2f);

        if ((maxCountInRow + 1) * _minXDistanceBetweenHolders > expectWidth)
        {
            expectWidth = (maxCountInRow + 1) * _minXDistanceBetweenHolders;
        }

        var height = expectWidth / aspect;


        var list = new List<Vector2>();
        var topRowMinPoint = transform.position + Vector3.up * height / 6f -
                             ((maxCountInRow - 1) / 2f) * _minXDistanceBetweenHolders * Vector3.right - Vector3.up*1f;
        list.AddRange(Enumerable.Range(0, maxCountInRow)
            .Select(i => (Vector2) topRowMinPoint + i * _minXDistanceBetweenHolders * Vector2.right));

        var lowRowMinPoint = transform.position - Vector3.up * height / 6f -
                             (((count - maxCountInRow) - 1) / 2f) * _minXDistanceBetweenHolders * Vector3.right - Vector3.up * 1f;
        list.AddRange(Enumerable.Range(0, count - maxCountInRow)
            .Select(i => (Vector2) lowRowMinPoint + i * _minXDistanceBetweenHolders * Vector2.right));

        return list;
    }


    public enum State
    {
        None,Playing,Over
    }

    public struct MoveData
    {
        public Holder FromHolder { get; set; }
        public Holder ToHolder { get; set; }
        public Ball Ball { get; set; }
    }
}

[Serializable]
public struct LevelGroup:IEnumerable<Level>
{
    public List<Level> levels;
    public IEnumerator<Level> GetEnumerator()
    {
        return levels?.GetEnumerator() ?? Enumerable.Empty<Level>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[Serializable]
public struct Level
{
    public int no;
    public List<LevelColumn> map;
}

[Serializable]
public struct LevelColumn : IEnumerable<int>
{
    public List<int> values;

    public IEnumerator<int> GetEnumerator()
    {
        return values?.GetEnumerator() ?? Enumerable.Empty<int>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}