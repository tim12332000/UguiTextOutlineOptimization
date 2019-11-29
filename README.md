參考資料 : https://www.cnblogs.com/GuyaWeiren/p/9665106.html
基本上整個做法8成以上都是參考這篇去微調的 感謝大大!!!

使用環境 Unity2018

為什麼要優化?
-------------------------------------------------------------------------------------------------------------------------
效能瓶頸: 
	其實UI畫面的 Vertex 數量不高一張圖為兩個3角形 ( 6 個 vertex )
	文字則是每個一個字元 ( 6 個vertex ) 想想一篇文章幾百個字的情況下已經是滿滿的 vertex 了， 在加上 Ugui Outline原件會讓文字的vertex x5 倍 
vertex 數量可以說是突破天際的高。

1. Ugui 自帶的 Outline 原件 Vertex x 5 倍 : 
Text Outline組件 , 實際他就是多畫四個文字在後面 , 偏移量設定多點就看出來了。(如圖)

![image](https://lh3.googleusercontent.com/Mzj66O0SY5qtJshXN_4M4NAAumiSLHNeV_vGLnYauERei9GQmq8Sr18dnhW4SaEzZCW9HcJjuoaC-bFGxzeXGV1zBuVO0l_vBdI9uRrHumNKov1USm_fiPkzj-vPd90I4DtX_Lkd)

2. Canvas 下的 vertices 數量最高不能超過 65000
超過會拋 "ArgumentException: Mesh can not have more than 65000 vertices"

以Unity的Outline來看一個字元 Outline就使用了 (3x2x5 = 30) 個 vertex , 所以最多只能放 ( 65000/30 = 2166..) 個字

解法：使用Shader做法替換
-------------------------------------------------------------------------------------------------------------------------
因為vertex消耗太高我們就把這部分放到 Fragment 階段實做。
使用Shader替換有個要求 DrawCall必須維持 1 
( 也有兩個 Pass的做法不建議使用。)
因為DrawCall 上升很大原因是因為 UIText很容易跟其他UI重疊是造成圖片無法Batch。
實際做法 : 
1.C#端 - 對 Vertex 範圍隨著黑框做放大 ( 讓黑框不會超出框框 )
2.Shader端 - 傳入 Outline 顏色 + Outline 寬度 &
計算 4個方向之後的底圖後再frag階段做疊加。

![image](https://lh3.googleusercontent.com/-06rdtHp7hQpqHMih3v6A7bvM-Us1eR54gBmz8RmOJ-JEr9AfjpuLsRWPzxtF39nRKL7DZ9RhQf_QHyxr8Kb90K_c-07Lt4whlWMEu5j2S9bXKZVjaW8RxUIpmpsChLhUzkYodP6)

可以看到 Vextex減少 ( 左圖用新方法使用只用了6 vertex )


新做法之限制:
-------------------------------------------------------------------------------------------------------------------------
1. 需要使用同個Material 所以框的顏色設定都相同。
2. 在做半透明字體時 框的透明度也要跟著調整 , 因為他們是分開計算的。
3. 隨著Outline的寬度調整 Vertex 寬度也會跟著變 請注意和其他圖片相疊的可能。
(相疊Batched會被中斷)

推薦使用方式: 
-------------------------------------------------------------------------------------------------------------------------
1. 先建立好共通的 Material 讓所有風格相同的文字代入。
2. 不建議動態new Material 這段也是很消耗效能。( 盡量共用同個Material )

效能測試:
-------------------------------------------------------------------------------------------------------------------------
共使用三種做法
1. UGUI Outline
2. 自定義Shader + C# 腳本
3. 自定義Shader + C# 腳本 + JobSystem
三種做法 Drawcall 相等。
效能 : 2 > 1 > 3
自定義Shader + C# 腳本 + JobSystem 最慢(不考慮)

![image](https://lh3.googleusercontent.com/mzsH0MdbLZ4JTcjVtR7BTCidv831lBNYexgomfSkqNTYesA-di0X6AmGBq2Nkion4uV5DT5Ieb-tVVjJ5x7rjnY1ti-fw6u9h1Kl5vhQ8dX96y3OxzZ7VjIgdsERviC2l9tGpoay)

關於Jobsystem 優化 (Unity2018)
-------------------------------------------------------------------------------------------------------------------------
在C# Script 裡的 vertex放大的計算做成併行計算，所以可以丟進多執行緒內並行。
已下使用JobSystem實測 :
	結果只能說慘@@? 因為Text的 UIVertex 數量不固定(所以不能Cache) 
不能Cache每次塞字進去就一定要 new NativeArray<UIVertex>() 
這段GC量也不少 (懷疑是這個關係 反而用了JobSystem 之後更慢了 ?_? )

![image](https://lh6.googleusercontent.com/Cy2V8X9F42BPyaGgGdEM4nZ0i2oKP20ppGO8ibnSEr5DYK3RuM2ZXbCpv6h-o9Oor8wmvd-MHyfD2XERejQEJWubDF9nejHLhKNFXyxhrW-OvNE8r5_hCxeJaqqwCLHT8k3gyXI9)

其他:
-------------------------------------------------------------------------------------------------------------------------
1.具體會不會因為把計算丟到Fragment 階段而多消耗多少效能 不是很清楚 ( 雖然估計是沒什麼差別 ) 主要是不太請楚這段怎麼測試?


