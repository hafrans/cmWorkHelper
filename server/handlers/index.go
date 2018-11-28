package handlers

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strings"
)

type IndexHandler struct{
	ProductId string
	Enable bool
        Limit string
        Notify bool
}


func (h *IndexHandler) ServeHTTP(w http.ResponseWriter, req *http.Request){

	url := req.RequestURI;
	switch {
	case strings.HasPrefix(url,"/index"):
		h.Index(w,req)
	case strings.HasPrefix(url,"/status"):
		h.Status(w,req)
	case strings.HasPrefix(url,"/update"):
		h.Update(w,req)
	case strings.HasPrefix(url,"/"):
		h.Index(w,req)
	default:
		http.NotFound(w,req)

	}

	log.Print(url);
	//fmt.Fprint(w,"This is a Index for CMWorkerHelper.")

}



func (h  IndexHandler) Index(w http.ResponseWriter, req *http.Request){
	//fmt.Fprint(w,"This is a Index for CMWorkerHelper.")
	http.ServeFile(w,req,"./static/index.htm")
}


func (h *IndexHandler) Status(w http.ResponseWriter, req *http.Request){

	w.Header().Set("Content-type","application/json")
	content , _ := json.Marshal(*h)
	fmt.Fprint(w,string(content))
}

func (h *IndexHandler) Update(w http.ResponseWriter, req *http.Request) {

	if h.Enable{
		h.Enable = false
	}else{
		h.Enable = true
	}

	w.Header().Set("Content-type","application/json")
	content , _ := json.Marshal(*h)
	fmt.Fprint(w,string(content))

}



