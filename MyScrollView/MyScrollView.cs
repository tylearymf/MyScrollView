using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public enum MyDragDirection
{
    Horizontal,
    Vertical
}

public class MyScrollView : MonoBehaviour
{

    int index = 0;
    SpringPanel sp;
    UIScrollView scrollView;
    Vector2 startDelta;     ///鼠标按下记录的起点坐标
    Vector2 endDelta;       ///鼠标抬起记录的重担坐标
#pragma warning disable
    Vector3 svcPos;         ///记录上一次ScrollView的本地坐标
    Vector3 startSvcPos;      ///记录ScrollView开始的本地坐标

    int maxPageOffsetX;     ///页面的宽度
    int maxPageOffsetY;     ///页面的高度
    int itemsCount;     ///item的集合
    int minPageOffsetX;     ///两个页面间距X轴偏移量
    int minPageOffsetY;     ///两个页面间距Y轴偏移量

    int miniDragX;      ///最小移动X轴阻尼
    int miniDragY;      ///最小移动Y轴阻尼

    List<GameObject> points;    ///点的 集合
    GameObject pointRoot;  ///点的父对象

    public float momentumAmount = 0.1F;  ///移动阻尼， 数值越小越容易移动
    public float itemOffsetX;   //item间距X轴偏移量
    public float itemOffsetY;   //item间距Y轴偏移量
    public int itemWidth;       //item宽度
    public int itemHeight;      //item高度
    public int cellCount;       //列数
    public int rowCount;        //行数
    public GameObject pointPrefab;      //点
    public int pointWidth;              //点的宽度
    public int pointHeight;             //点的高度
    public Transform pointPos;          //点的位置
    public GameObject nullItem;         //空item

    public delegate void OnDragFinished(int index);
    public event OnDragFinished ondragFinished;

    [HideInInspector]
    [SerializeField]
    MyDragDirection _dragDirec = MyDragDirection.Horizontal;
    public MyDragDirection dragDirec
    {
        set { _dragDirec = value; }
        get { return _dragDirec; }
    }

    [HideInInspector]
    [SerializeField]
    float _pageOffsetX;
    public float pageOffsetX
    {
        set { _pageOffsetX = value; }
        get { return _pageOffsetX; }
    }

    [HideInInspector]
    [SerializeField]
    float _pageOffsetY;
    public float pageOffsetY
    {
        set { _pageOffsetY = value; }
        get { return _pageOffsetY; }
    }

    [HideInInspector]
    [SerializeField]
    float _pointOffsetX;
    public float pointOffsetX
    {
        set { _pointOffsetX = value; }
        get { return _pointOffsetX; }
    }

    [HideInInspector]
    [SerializeField]
    float _pointOffsetY;
    public float pointOffsetY
    {
        set { _pointOffsetY = value; }
        get { return _pointOffsetY; }
    }

    void Start()
    {
        items = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            GameObject g = Myutils.makeGameObject(ItemPrefab, ItemPos);
            g.name = i.ToString();
            items.Add(g);
        }
        ItemLayout(items);
    }

    LinkedList<GameObject> linkedObjects = new LinkedList<GameObject>();

    public void ItemLayout(List<GameObject> _items)
    {
        if (_items == null || _items.Count <= 0)
        {
            Debug.LogError("UIScrollView Items为空");
            return;
        }

        if (rowCount == 0 || cellCount == 0)
        {
            Debug.LogError("行数或列数不能为0！！");
            return;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            linkedObjects.AddLast(_items[i]);
        }

        pointRoot = pointPos.transform.parent.gameObject;
        startSvcPos = gameObject.transform.localPosition;
        svcPos = gameObject.transform.localPosition;
        scrollView = gameObject.GetComponent<UIScrollView>();
        scrollView.SetDragScrollView = false;
        scrollView.onDragStarted += new UIScrollView.OnDragNotification(onDragStarted);
        scrollView.onDragFinished += new UIScrollView.OnDragNotification(onDragFinished);

        var maxPageIndex = 0;
        for (var index = 0; index++ * (cellCount * rowCount) < _items.Count;)
        {
            maxPageIndex++;
        }

        var missItemsCount = maxPageIndex * cellCount * rowCount - _items.Count;
        GameObject g = null;
        for (var index = 0; index < missItemsCount; index++)
        {
            g = makeGameobject(nullItem, _items[0].transform);
            _items.Add(g);
        }

        var _numIndex = 0;
        var _rowIndex = 0;
        var _cellIndex = 0;
        var pageIndex = 0;
        var x = .0F;
        var y = .0F;
        var z = .0F;
        foreach (var item in _items)
        {
            if (dragDirec == MyDragDirection.Horizontal)
            {
                if (_numIndex % cellCount == 0 && _numIndex != 0)
                {
                    _cellIndex++;
                    _rowIndex = pageIndex * cellCount;
                }

                if (_numIndex % (cellCount * rowCount) == 0 && _numIndex != 0)
                {
                    _cellIndex = 0;
                    pageIndex++;
                    _rowIndex = pageIndex * cellCount;
                }

                x = item.transform.localPosition.x + _rowIndex * itemOffsetX + pageIndex * _pageOffsetX;
                y = item.transform.localPosition.y + _cellIndex * itemOffsetY + pageIndex * _pageOffsetY;
                z = item.transform.localPosition.z;
            }
            if (dragDirec == MyDragDirection.Vertical)
            {
                if (_numIndex % cellCount == 0 && _numIndex != 0)
                {
                    _cellIndex++;
                    _rowIndex = 0;
                }

                if (_numIndex % (cellCount * rowCount) == 0 && _numIndex != 0)
                {
                    pageIndex++;
                }

                x = item.transform.localPosition.x + _rowIndex * itemOffsetX + pageIndex * _pageOffsetX;
                y = item.transform.localPosition.y + _cellIndex * itemOffsetY + pageIndex * _pageOffsetY;
                z = item.transform.localPosition.z;
            }
            item.transform.localPosition = new Vector3(x, y, z);

            _numIndex++;
            _rowIndex++;
        }

        if (dragDirec == MyDragDirection.Horizontal)
        {
            maxPageOffsetX = (int)itemOffsetX * cellCount;
            miniDragX = (int)(maxPageOffsetX * momentumAmount);
        }
        if (dragDirec == MyDragDirection.Vertical)
        {
            maxPageOffsetY = (int)itemOffsetY * rowCount;
            miniDragY = (int)(maxPageOffsetY * momentumAmount);
        }
        itemsCount = Mathf.CeilToInt(((_items.Count * 0.1F) / (cellCount * rowCount)) * 10);
        minPageOffsetX = (int)pageOffsetX;
        minPageOffsetY = (int)pageOffsetY;

        points = new List<GameObject>();
        GameObject point = null;
        for (int i = 0; i < itemsCount; i++)
        {
            point = makeGameobject(pointPrefab, pointPos);
            point.transform.localPosition += new Vector3(i * pointOffsetX, i * pointOffsetY, 0);
            points.Add(point);
        }

        //pointRoot.transform.localPosition = new Vector3(-0.5F * itemsCount * pointOffsetX, -0.5F * itemsCount * pointOffsetY, 0);
        //pointRoot.transform.position = pointPos.position;

        if (dragDirec == MyDragDirection.Horizontal)
        {
            pointRoot.transform.localPosition += new Vector3(-.5F * (itemsCount * pointOffsetX + (pointWidth - pointOffsetX) - pointWidth), 0, 0);
        }

        if (dragDirec == MyDragDirection.Vertical)
        {
            pointRoot.transform.localPosition += new Vector3(0, -.5F * (itemsCount * pointOffsetY + (pointHeight - pointOffsetY) - pointHeight), 0);
        }

        ChangePoint();
    }

    public static GameObject makeGameobject(GameObject go, Transform tr)
    {
        GameObject gb = (GameObject)Instantiate(go);
        gb.transform.parent = tr.parent;
        gb.transform.localPosition = tr.localPosition;
        gb.transform.localScale = tr.localScale;
        gb.transform.localEulerAngles = Vector3.zero;
        return gb;
    }

    void ChangePoint(int index = 0)
    {
        if (points == null) return;
        foreach (var item in points)
        {
            item.GetComponent<point>().Reset();
        }

        if (index >= 0 && index < points.Count) points[index].GetComponent<point>().Change();
    }

    void onDragFinished()
    {
        endDelta = scrollView.transform.localPosition;
        //Debug.Log("拖拽完成");
        ChangePos();
    }

    void onDragStarted()
    {
        startDelta = scrollView.transform.localPosition;
        //Debug.Log("拖拽开始");
    }

    enum Direction
    {
        Left,
        Right
    }

    public GameObject ItemPrefab;
    public Transform ItemPos;
    Direction direction;

    /// <summary>
    /// 存储所显示的对象
    /// </summary>
    List<GameObject> items;
    /// <summary>
    /// 记录当前所指向的对象
    /// </summary>
    GameObject currentItem;

    int oldIndex;
    int totalCount = 10;
    void AddAndRemoveLinkedList()
    {
        if (oldIndex == index) return;

        Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaa" + ";;;" + direction.ToString());
        GameObject item;
        switch (direction)
        {
            case Direction.Left:

                if (linkedObjects.Find(currentItem).Next == null)
                {
                    Debug.Log("到达最后一个节点！！！");

                    int leftIndex = index + 1;
                    if (leftIndex >= totalCount) break;

                    item = Myutils.makeGameObject(ItemPrefab, ItemPos);
                    item.transform.SetLocalX(item.transform.localPosition.x + leftIndex * itemOffsetX);
                    item.name = leftIndex.ToString();
                    linkedObjects.AddLast(item);
                    items.Add(item);

                    GameObject first = linkedObjects.First.Value;
                    if (first != null)
                    {
                        Debug.Log("first=" + first.name);
                        Destroy(first);
                        items.RemoveAt(0);
                    }
                    linkedObjects.RemoveFirst();
                }
                break;
            case Direction.Right:

                if (linkedObjects.Find(currentItem).Previous == null)
                {
                    Debug.Log("到达第一个节点！！！");

                    int rightIndex = index - 1;
                    if (rightIndex < 0) break;

                    item = Myutils.makeGameObject(ItemPrefab, ItemPos);
                    item.transform.SetLocalX(item.transform.localPosition.x + rightIndex * itemOffsetX);
                    item.name = rightIndex.ToString();
                    linkedObjects.AddFirst(item);
                    items.Insert(0, item);

                    GameObject last = linkedObjects.Last.Value;
                    if (last != null)
                    {
                        Debug.Log("last=" + last.name);
                        Destroy(last);
                        items.RemoveAt(3);
                    }
                    linkedObjects.RemoveLast();
                }
                break;
        }
    }

    public void ChangePos()
    {
        int x = (int)(endDelta.x - startDelta.x);
        int y = (int)(endDelta.y - startDelta.y);
        //Debug.Log(index + "###" + x);

        if (index >= totalCount) index = 0;

        if (dragDirec == MyDragDirection.Horizontal)
        {
            if (x > 0 && x > miniDragX && (index - 1) >= 0)
            {
                oldIndex = index;
                index--;
                direction = Direction.Right;
                SpringBegin();
            }
            else if (x < 0 && x < (-1 * miniDragX) && (index + 1) < totalCount)
            {
                oldIndex = index;
                index++;
                direction = Direction.Left;
                SpringBegin();
            }
            else
            {
                SpringBegin();
            }
        }

        if (dragDirec == MyDragDirection.Vertical)
        {
            if (y < 0 && y < (-1 * miniDragY) && (index - 1) >= 0)
            {
                oldIndex = index;
                index--;
                direction = Direction.Right;
                SpringBegin();
            }
            else if (y > 0 && y > miniDragY && (index + 1) < totalCount)
            {
                oldIndex = index;
                index++;
                direction = Direction.Left;
                SpringBegin();
            }
            else
            {
                SpringBegin();
            }
        }
    }

    void SpringBegin(int strength = 8)
    {
        sp = gameObject.GetComponent<SpringPanel>();
        if (sp == null) sp = gameObject.AddComponent<SpringPanel>();

        Vector3 vec = Vector3.zero;

        if (dragDirec == MyDragDirection.Horizontal)
        {
            vec = new Vector3(-1 * (maxPageOffsetX + minPageOffsetX) * index + startSvcPos.x, startSvcPos.y, 0);
        }

        if (dragDirec == MyDragDirection.Vertical)
        {
            vec = new Vector3(startSvcPos.x, -1 * (maxPageOffsetY + minPageOffsetY) * index + startSvcPos.y, 0);
        }
        //Debug.Log(index + "---"+vec);
        svcPos = vec;
        sp.target = vec;
        sp.strength = strength;
        sp.enabled = true;
        sp.finished = AddAndRemoveLinkedList;

        ////改变标识点
        ChangePoint(index);
        if (ondragFinished != null)
        {
            ondragFinished(index);
        }

        int firstID = Convert.ToInt32(items[0].name);
        currentItem = items[index - firstID];
        Debug.Log("currentItem=" + currentItem.name);
    }

    /// <summary>
    /// 重置页面坐标
    /// </summary>
    public void ResetPosition()
    {
        //Debug.Log("OnResetPage");
        index = 0;
        SpringBegin(20);
        ChangePoint();
    }
}
