var http = require('http');
fs = require('fs');
ejs = require('ejs');
qs = require('querystring');
var settings = require('./settings');
var server = http.createServer();
//var msg;
//var template = fs.readFileSync(__dirname + '/public_html/bbs.ejs', 'utf-8');
//var n = 0;
//var posts = [];
var comments = [];
MongoClient = require('mongodb').MongoClient;

MongoClient.connect("mongodb://localhost/" + settings.db, function (err, db) {
    if (err) { return console.dir(err); }
    console.log("connected to db");
    //TODO:DBから配列にデータ読み込み
    db.collection("rebellion", function (err, collection) { //叛逆用のcollectionに接続
        collection.find().sort({ date: -1 }).limit(5000).toArray(function (err, items) { //データを新しい順から5000件取得
            for (var i = 0; i < items.length; i++) { //全部配列に追加
                comments.push({ time: items[i].time, date: items[i].date, comment: items[i].comment })
            }
            console.log("readnum" + items.length);
        });
        /*var stream = collection.find().limit(5000).stream(); //コメントの最大数は5000
        stream.on("data", function (item) { //読み込み完了
            console.log("readnum：" + item);
            for (var i = 0; i < item.length; i++) { //全部配列に追加
                comments.Add({ time: item[i].time, date:item[i].date, comment:item[i].comment})
            }
        });
        stream.on("end", function () {
            console.log("finished.");
        });*/
    });
});

/*
function renderForm(posts, res) {
    var data = ejs.render(template, {
        posts: posts
    });
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.write(data);
    res.end();
}*/

server.on('request', function (req, res) {
    if (req.method === 'POST') { //コメントがPOSTされた
        req.data = "";
        req.on("readable", function () {//POSTデータ読み込み中
            req.data += req.read() || '';
        });
        req.on("end", function () { //POSTデータ読み込み完了
            var query = qs.parse(req.data);
            var docs = { time: query.time, date: Date.now(), comment: query.comment };
            console.log(docs);
            comments.push(docs); //配列に追加

            MongoClient.connect("mongodb://localhost/" + settings.db, function (err, db) {
                if (err) { return console.dir(err); }
                //console.log("query:" + req.data);
                //mongoDBに追加(コレクション名はquery.video)
                db.collection(query.video, function (err, collection) {
                    collection.insert(docs, function (err, result) {
                        //console.dir(result);
                    });
                });
            });

            var data = "OK!";
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.write(data);
            res.end();

            //5000件超えたら先頭を削除
            if (comments.length > 5000) {
                comments.shift(); //先頭を削除
            }
        });
    } else {//GETの場合
        //console.log("GETきた");
        var data = JSON.stringify(comments); //コメントをJSONに変換
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.write(data);
        res.end();
    }
    /*n++;
    var data = ejs.render(template, {
        title: "hello",
        content: "<strong>world!</strong>",
        n: n
    });
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.write(data);
    res.end();*/
	/*fs.readFile(__dirname + '/public_html/hello.html', 'utf-8', function(err, data) {
		if(err){
			res.writeHead(404, {'Content-Type': 'text/plain'});
			res.write("not found!");
			res.end();
		}
		res.writeHead(200, {'Content-Type': 'text/html'});
		res.write(data);
		res.end();
	});*/
	/*switch (req.url){
		case '/about':
			msg = "about this page";
			break;
		case '/profile':
			msg = "about me";
			break;
		default:
			msg = 'wrong page';
			break;
	}*/
});
server.listen(settings.port, settings.host);
console.log("server listening...");