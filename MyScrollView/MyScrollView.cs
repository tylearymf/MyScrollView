using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum MyDragDirection
{
    Horizontal,
    Vertical
}

public class MyScrollView : MonoBehaviour
{

    private int index = 0;
    private SpringPanel sp;
    private UIScrollView scrollView;
    private Vector2 startDelta;     ///鼠标按下记录的起点坐标
    private Vector2 endDelta;       ///鼠标抬起记录的重担坐标
    private Vector3 svcPos;         ///记录上一次ScrollView的本地坐标
    private Vector3 startSvcPos;      ///记录ScrollView开始的本地坐标

    private int maxPageOffsetX;     ///页面的宽度
    private int maxPageOffsetY;     ///页面的高度
    private int itemsCount;     ///item的集合
    private int minPageOffsetX;     ///两个页面间距X轴偏移量
    private int minPageOffsetY;     ///两个页面间距Y轴偏移量

    private int miniDragX;      ///最小移动X轴阻尼
    private int miniDragY;      ///最小移动Y轴阻尼

    private List<GameObject> points;    ///点的 集合
    private GameObject pointRoot;  ///点的父对象

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
    private MyDragDirection _dragDirec = MyDragDirection.Horizontal;
    public MyDragDirection dragDirec
    {
        set { _dragDirec = value; }
        get { return _dragDirec; }
    }

    [HideInInspector]
    [SerializeField]
    private float _pageOffsetX;
    public float pageOffsetX
    {
        set { _pageOffsetX = value; }
        get { return _pageOffsetX; }
    }

    [HideInInspector]
    [SerializeField]
    private float _pageOffsetY;
    public float pageOffsetY
    {
        set { _pageOffsetY = value; }
        get { return _pageOffsetY; }
    }

    [HideInInspector]
    [SerializeField]
    private float _pointOffsetX;
    public float pointOffsetX
    {
        set { _pointOffsetX = value; }
        get { return _pointOffsetX; }
    }

    [HideInInspector]
    [SerializeField]
    private float _pointOffsetY;
    public float pointOffsetY
    {
        set { _pointOffsetY = value; }
        get { return _pointOffsetY; }
    }

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

    private void ChangePoint(int index = 0)
    {
        if (points == null) return;
        foreach (var item in points)
        {
            item.GetComponent<point>().Reset();
        }

        if (index >= 0 && index < points.Count) points[index].GetComponent<point>().Change();
    }

    private void onDragFinished()
    {
        endDelta = scrollView.transform.localPosition;
        //Debug.Log("拖拽完成");
        ChangePos();
    }

    private void onDragStarted()
    {
        startDelta = scrollView.transform.localPosition;
        //Debug.Log("拖拽开始");
    }

    public void ChangePos()
    {
        int x = (int)(endDelta.x - startDelta.x);
        int y = (int)(endDelta.y - startDelta.y);
        //Debug.Log(index + "###" + x);

        if (index >= itemsCount) index = 0;

        if (dragDirec == MyDragDirection.Horizontal)
        {
            if (x > 0 && x > miniDragX && (index - 1) >= 0)
            {
                index--;
                SpringBegin();
            }
            else if (x < 0 && x < (-1 * miniDragX) && (index + 1) < itemsCount)
            {
                index++;
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
                index--;
                SpringBegin();
            }
            else if (y > 0 && y > miniDragY && (index + 1) < itemsCount)
            {
                index++;
                SpringBegin();
            }
            else
            {
                SpringBegin();
            }
        }
    }

    private void SpringBegin(int strength = 8)
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

        ////改变标识点
        ChangePoint(index);
        if (ondragFinished != null)
        {
            ondragFinished(index);
        }
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
