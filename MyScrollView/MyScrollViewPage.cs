using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MyScrollViewPage : MonoBehaviour
{
    int index = 0;
    SpringPanel sp;
    UIScrollView scrollView;
    Vector2 startDelta;     //鼠标按下记录的起点坐标
    Vector2 endDelta;       //鼠标抬起记录的重担坐标
#pragma warning disable
    Vector3 svcPos;         //记录上一次ScrollView的本地坐标
    Vector3 startSvcPos;      //记录ScrollView开始的本地坐标

    int maxPageOffsetX;     //页面的宽度
    int maxPageOffsetY;     //页面的高度
    int itemsCount;     //item的集合
    int minPageOffsetX;     //两个页面间距X轴偏移量
    int minPageOffsetY;     //两个页面间距Y轴偏移量

    int miniDragX;      //最小移动X轴阻尼
    int miniDragY;      //最小移动Y轴阻尼

    public float MomentumAmount = 0.1F;  //移动阻尼， 数值越小越容易移动
    public float GenerateMomentumAmount = 50F;  //生成对象阻尼， 数值越大越快生成
    public float ItemOffsetX;   //item间距X轴偏移量
    public float ItemOffsetY;   //item间距Y轴偏移量
    public int ItemWidth;       //item宽度
    public int ItemHeight;      //item高度
    public int CellCount;       //列数
    public int RowCount;        //行数
    public GameObject NullItem;         //空item
    public UILabel CurrentIndexLabel;   //当前页面索引
    public UILabel TotalCountLabel;     //页面数量之和

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
        if (!isSetPageTotalCount)
        {
            Debug.LogError("未设置总页面数量！！！");
            return;
        }

        sp = gameObject.GetComponent<SpringPanel>();
        if (sp == null) sp = gameObject.AddComponent<SpringPanel>();
        sp.GenerateMomentumAmount = GenerateMomentumAmount;

        items = new List<GameObject>();
        //pageStartCount = 0;
        pageStartCount = CellCount * RowCount * 3;

        for (int i = 0; i < pageStartCount;)
        {
            GameObject g = Myutils.makeGameObject(ItemPrefab, ItemPos);
            g.name = i.ToString();
            items.Add(g);

            if (AssignEvent != null)
                AssignEvent(g, i++);
        }

        if (items != null && items.Count > 0)
        {
            currentStartItem = items[0];
            currentEndItem = items[items.Count - 1];

            ItemLayout(items);
        }

        #region 协程处理
        //startMakeID = 0;
        //StartCoroutine(TimeOutMakeItems());
        #endregion
    }

    int pageStartCount;
    #region 协程处理
    //int startMakeID;
    //IEnumerator TimeOutMakeItems()
    //{
    //    if(startMakeID >= 0 && startMakeID < pageStartCount)
    //    {
    //        GameObject g = Myutils.makeGameObject(ItemPrefab, ItemPos);
    //        g.name = startMakeID.ToString();
    //        items.Add(g);

    //        if (AssignEvent != null)
    //            AssignEvent(g, startMakeID++);

    //        yield return new  WaitForEndOfFrame();
    //        //yield return new WaitForEndOfFrame();
    //        StartCoroutine(TimeOutMakeItems());
    //    }
    //    else
    //    {
    //        if (items != null && items.Count > 0)
    //        {
    //            currentStartItem = items[0];
    //            currentEndItem = items[items.Count - 1];

    //            ItemLayout(items);
    //        }
    //    }
    //}
    #endregion

    LinkedList<GameObject> linkedObjects = new LinkedList<GameObject>();
    public delegate void ItemDelegate(GameObject item, int id);
    public event ItemDelegate AssignEvent;

    void ItemLayout(List<GameObject> _items)
    {
        if (_items == null || _items.Count <= 0)
        {
            Debug.LogError("UIScrollView Items为空");
            return;
        }

        if (RowCount == 0 || CellCount == 0)
        {
            Debug.LogError("行数或列数不能为0！！");
            return;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            linkedObjects.AddLast(_items[i]);
        }

        startSvcPos = gameObject.transform.localPosition;
        svcPos = gameObject.transform.localPosition;
        scrollView = gameObject.GetComponent<UIScrollView>();
        scrollView.SetDragScrollView = false;
        scrollView.onDragStarted += new UIScrollView.OnDragNotification(onDragStarted);
        scrollView.onDragFinished += new UIScrollView.OnDragNotification(onDragFinished);

        Layout(_items);

        if (dragDirec == MyDragDirection.Horizontal)
        {
            maxPageOffsetX = (int)ItemOffsetX * CellCount;
            miniDragX = (int)(maxPageOffsetX * MomentumAmount);
        }
        if (dragDirec == MyDragDirection.Vertical)
        {
            maxPageOffsetY = (int)ItemOffsetY * RowCount;
            miniDragY = (int)(maxPageOffsetY * MomentumAmount);
        }
        itemsCount = Mathf.CeilToInt(((_items.Count * 0.1F) / (CellCount * RowCount)) * 10);
        minPageOffsetX = (int)pageOffsetX;
        minPageOffsetY = (int)pageOffsetY;

        ChangePoint();
    }

    void Layout(List<GameObject> _items, int pageIndex = 0, int _numIndex = 0)
    {
        var maxPageIndex = 0;
        for (var index = 0; index++ * (CellCount * RowCount) < _items.Count;)
        {
            maxPageIndex++;
        }
        var missItemsCount = maxPageIndex * CellCount * RowCount - _items.Count;
        GameObject g = null;
        for (var index = 0; index < missItemsCount; index++)
        {
            g = makeGameobject(NullItem, _items[0].transform);
            _items.Add(g);
        }

        //var _numIndex = 0;
        var _rowIndex = 0;
        var _cellIndex = 0;
        //var pageIndex = 0;
        var x = .0F;
        var y = .0F;
        var z = .0F;
        foreach (var item in _items)
        {
            if (dragDirec == MyDragDirection.Horizontal)
            {
                if (_numIndex % CellCount == 0 && _numIndex != 0)
                {
                    _cellIndex++;
                    _rowIndex = pageIndex * CellCount;
                }

                if (_numIndex % (CellCount * RowCount) == 0 && _numIndex != 0)
                {
                    _cellIndex = 0;
                    pageIndex++;
                    _rowIndex = pageIndex * CellCount;
                }

                x = item.transform.localPosition.x + _rowIndex * ItemOffsetX + pageIndex * _pageOffsetX;
                y = item.transform.localPosition.y + _cellIndex * ItemOffsetY + pageIndex * _pageOffsetY;
                z = item.transform.localPosition.z;
            }
            if (dragDirec == MyDragDirection.Vertical)
            {
                if (_numIndex % CellCount == 0 && _numIndex != 0)
                {
                    _cellIndex++;
                    _rowIndex = 0;
                }

                if (_numIndex % (CellCount * RowCount) == 0 && _numIndex != 0)
                {
                    pageIndex++;
                }

                x = item.transform.localPosition.x + _rowIndex * ItemOffsetX + pageIndex * _pageOffsetX;
                y = item.transform.localPosition.y + _cellIndex * ItemOffsetY + pageIndex * _pageOffsetY;
                z = item.transform.localPosition.z;
            }
            item.transform.localPosition = new Vector3(x, y, z);

            _numIndex++;
            _rowIndex++;
        }
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
        if (CurrentIndexLabel != null)
            CurrentIndexLabel.text = (index + 1).ToString();
        if (TotalCountLabel != null)
            TotalCountLabel.text = pageTotalCount.ToString();
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
    /// 记录当前页所指向的最后一个对象
    /// </summary>
    GameObject currentEndItem;
    /// <summary>
    /// 记录当前页所指向的第一个对象
    /// </summary>
    GameObject currentStartItem;

    bool isSetPageTotalCount = false;
    int itemTotalCount;
    /// <summary>
    /// 总共多少页
    /// </summary>
    int pageTotalCount = 10;
    /// <summary>
    /// 传入Item的Count即可
    /// </summary>
    public int PageTotalCount
    {
        set
        {
            if (value > 0)
            {
                itemTotalCount = value;
                int count = Mathf.CeilToInt(value / (CellCount * RowCount * 1.0F));

                pageTotalCount = count;
                isSetPageTotalCount = true;
            }
            else
                Debug.LogError("设置错误！！！");
        }
    }

    #region 协程处理
    //GameObject item;
    //int pageCount;

    //int oldLeftIndex;
    //int leftIndex;
    //List<GameObject> tempMakeLeftItems;
    //int makeLeftID;

    //IEnumerator TimeOutMakeLeftItems()
    //{
    //    if (makeLeftID >= 0 && makeLeftID < pageCount)
    //    {
    //        int id = (leftIndex * pageCount) + makeLeftID++;

    //        if (id < itemTotalCount)
    //        {
    //            item = Myutils.makeGameObject(ItemPrefab, ItemPos);
    //            item.name = id.ToString();
    //            tempMakeLeftItems.Add(item);
    //            linkedObjects.AddLast(item);
    //            items.Add(item);

    //            if (AssignEvent != null)
    //                AssignEvent(item, id);
    //        }
    //        else
    //        {
    //            item = Myutils.makeGameObject(NullItem, ItemPos);
    //            item.name = id.ToString();
    //            tempMakeLeftItems.Add(item);
    //            linkedObjects.AddLast(item);
    //            items.Add(item);
    //        }

    //        GameObject first = linkedObjects.First.Value;
    //        if (first != null)
    //        {
    //            Destroy(first);
    //            items.RemoveAt(0);
    //        }
    //        linkedObjects.RemoveFirst();

    //        yield return new WaitForEndOfFrame();
    //        //yield return new WaitForSeconds(1F);
    //        //yield return new WaitForEndOfFrame();
    //        StartCoroutine(TimeOutMakeLeftItems());
    //    }
    //    else
    //    {
    //        Layout(tempMakeLeftItems, oldLeftIndex, oldLeftIndex * pageCount);

    //    }
    //}

    //int oldRightIndex;
    //int rightIndex;
    //List<GameObject> tempMakeRightItems;
    //int makeRightID;
    //IEnumerator TimeOutMakeRightItems()
    //{
    //    if (makeRightID >= 0 && makeRightID < pageCount)
    //    {
    //        item = Myutils.makeGameObject(ItemPrefab, ItemPos);
    //        int id = (rightIndex * pageCount) + makeRightID++;
    //        item.name = id.ToString();
    //        tempMakeRightItems.Add(item);
    //        linkedObjects.AddFirst(item);
    //        items.Insert(0, item);

    //        if (AssignEvent != null)
    //            AssignEvent(item, id);

    //        GameObject last = linkedObjects.Last.Value;
    //        if (last != null)
    //        {
    //            Destroy(last);
    //            items.RemoveAt(items.Count - 1);
    //        }
    //        linkedObjects.RemoveLast();

    //        yield return new WaitForEndOfFrame();
    //        //yield return new WaitForSeconds(1F);
    //        //yield return new WaitForEndOfFrame();
    //        StartCoroutine(TimeOutMakeRightItems());
    //    }
    //    else
    //    {
    //        Layout(tempMakeRightItems, oldRightIndex, rightIndex * pageCount);
    //    }

    //}

    //void AddAndRemoveLinkedList()
    //{
    //    lock (this)
    //    {
    //        pageCount = CellCount * RowCount;
    //        switch (direction)
    //        {
    //            case Direction.Left:

    //                oldLeftIndex = index;
    //                leftIndex = index + 1;
    //                if (leftIndex >= pageTotalCount) break;

    //                if (linkedObjects.Find(currentEndItem).Next == null)
    //                {
    //                    //Debug.Log("到达最后一个节点！！！" + "---Index=" + index);
    //                    makeLeftID = 0;
    //                    tempMakeLeftItems = new List<GameObject>();
    //                    tempMakeLeftItems.Clear();
    //                    StartCoroutine(TimeOutMakeLeftItems());
    //                }
    //                break;
    //            case Direction.Right:

    //                oldRightIndex = index - 2 < 0 ? 0 : index - 2;
    //                rightIndex = index - 1;
    //                if (rightIndex < 0) break;

    //                if (linkedObjects.Find(currentStartItem).Previous == null)
    //                {
    //                    //Debug.Log("到达第一个节点！！！" + "---Index=" + index);
    //                    makeRightID = 0;
    //                    tempMakeRightItems = new List<GameObject>();
    //                    tempMakeRightItems.Clear();
    //                    StartCoroutine(TimeOutMakeRightItems());
    //                }
    //                break;
    //        }
    //    }
    //}

    #endregion

    void AddAndRemoveLinkedList()
    {
        lock (this)
        {
            GameObject item;
            switch (direction)
            {
                case Direction.Left:

                    int oldLeftIndex = index;
                    int leftIndex = index + 1;
                    if (leftIndex >= pageTotalCount) break;

                    if (linkedObjects.Find(currentEndItem).Next == null)
                    {
                        //Debug.Log("到达最后一个节点！！！" + "---Index=" + index);

                        int pageCount = CellCount * RowCount;
                        List<GameObject> tempItems = new List<GameObject>();
                        for (int i = 0; i < pageCount; i++)
                        {
                            int id = (leftIndex * pageCount) + i;

                            if (id < itemTotalCount)
                            {
                                if (gameObject.activeInHierarchy)
                                {
                                    item = Myutils.makeGameObject(ItemPrefab, ItemPos);
                                    item.name = id.ToString();
                                    tempItems.Add(item);
                                    linkedObjects.AddLast(item);
                                    items.Add(item);

                                    if (AssignEvent != null)
                                        AssignEvent(item, id);
                                }
                            }
                            else
                            {
                                item = Myutils.makeGameObject(NullItem, ItemPos);
                                item.name = id.ToString();
                                tempItems.Add(item);
                                linkedObjects.AddLast(item);
                                items.Add(item);
                            }

                            GameObject first = linkedObjects.First.Value;
                            if (first != null)
                            {
                                //Debug.Log("first=" + first.name);
                                Destroy(first);
                                items.RemoveAt(0);
                            }
                            linkedObjects.RemoveFirst();
                        }

                        //Debug.Log("oldLeftIndex=" + oldLeftIndex);
                        Layout(tempItems, oldLeftIndex, oldLeftIndex * pageCount);
                    }
                    break;
                case Direction.Right:

                    int oldRightIndex = index - 2 < 0 ? 0 : index - 2;
                    int rightIndex = index - 1;
                    if (rightIndex < 0) break;

                    if (linkedObjects.Find(currentStartItem).Previous == null)
                    {
                        //Debug.Log("到达第一个节点！！！" + "---Index=" + index);

                        int pageCount = CellCount * RowCount;
                        List<GameObject> tempItems = new List<GameObject>();
                        for (int i = 0; i < pageCount; i++)
                        {
                            if (gameObject.activeInHierarchy)
                            {
                                item = Myutils.makeGameObject(ItemPrefab, ItemPos);
                                int id = (rightIndex * pageCount) + i;
                                item.name = id.ToString();
                                tempItems.Add(item);
                                linkedObjects.AddFirst(item);
                                items.Insert(0, item);

                                if (AssignEvent != null)
                                    AssignEvent(item, id);

                            }
                            GameObject last = linkedObjects.Last.Value;
                            if (last != null)
                            {
                                //Debug.Log("first=" + last.name);
                                Destroy(last);
                                items.RemoveAt(items.Count - 1);
                            }
                            linkedObjects.RemoveLast();
                        }

                        Debug.Log("生成PageIndex=" + oldRightIndex + "----生成NumIndex=" + rightIndex * pageCount + "----Index=" + index);
                        Layout(tempItems, oldRightIndex, rightIndex * pageCount);
                    }
                    break;
            }
        }
    }

    public void ChangePos()
    {
        int x = (int)(endDelta.x - startDelta.x);
        int y = (int)(endDelta.y - startDelta.y);
        //Debug.Log(index + "###" + x);

        if (index >= pageTotalCount) index = 0;

        if (dragDirec == MyDragDirection.Horizontal)
        {
            if (x > 0 && x > miniDragX && (index - 1) >= 0 && linkedObjects.Find(currentStartItem).Previous != null)
            {
                index--;
                direction = Direction.Right;
                SpringBegin();
            }
            else if (x < 0 && x < (-1 * miniDragX) && (index + 1) < pageTotalCount && linkedObjects.Find(currentEndItem).Next != null)
            {
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
            if (y < 0 && y < (-1 * miniDragY) && (index - 1) >= 0 && linkedObjects.Find(currentStartItem).Previous != null)
            {
                index--;
                direction = Direction.Right;
                SpringBegin();
            }
            else if (y > 0 && y > miniDragY && (index + 1) < pageTotalCount && linkedObjects.Find(currentEndItem).Next != null)
            {
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

        int pageCount = CellCount * RowCount;
        int firstID = Convert.ToInt32(items[0].name);
        firstID = firstID / pageCount;  //1
        int itemsIndex = (index - firstID) * pageCount;
        if (itemsIndex >= 0 && itemsIndex < items.Count)
        {
            currentStartItem = items[itemsIndex];
            //Debug.Log("currentStartItem=" + currentStartItem.name);
        }

        itemsIndex = itemsIndex + (pageCount - 1);
        if (itemsIndex >= 0 && itemsIndex < items.Count)
        {
            currentEndItem = items[itemsIndex];
            //Debug.Log("currentEndItem=" + currentEndItem.name);
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
