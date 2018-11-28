package main

import (
	"log"
	"net/http"
	"github.com/hafrans/cmWorkHelper/server/handlers"
)

func main(){

	//http.HandleFunc()
	http.Handle("/sp/",&handlers.StaticHandler{"./static","/sp/"})
	http.Handle("/",&handlers.IndexHandler{"fuse",false,"2019",false})
	log.Print("Server is runing. at 0.0.0.0:15536.");
	if err := http.ListenAndServe("0.0.0.0:15536",nil); err != nil{
		log.Fatal(err);
	}else{
		log.Print("Server is runing. at 0.0.0.0:15536.");
	}





}
