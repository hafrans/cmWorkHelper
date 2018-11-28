package handlers

import (
	"log"
	"net/http"
	"os"
	"strings"
)

func init(){
	log.Print("Load Static Handler")
}


type StaticHandler struct{
	Path string
	RequestPath string

}


func (h *StaticHandler) fileExists(path string) bool{
	 _, err := os.Stat(path)
	log.Print(path)
	if err!=nil{
		log.Print(err)
	}
	return err == nil

}


func (h *StaticHandler) ServeHTTP(w http.ResponseWriter, r *http.Request){
	log.Print(r.RequestURI)
	urlString := r.RequestURI

	if !strings.HasPrefix(urlString,h.RequestPath){
		w.WriteHeader(http.StatusForbidden)
		w.Write([]byte("Request Forbidden"))
		return
	}

	if strings.Contains(urlString,".."){
		w.WriteHeader(http.StatusForbidden)
		w.Write([]byte("Request Forbidden"))
		return
	}

	truePath := h.Path + urlString[len(h.RequestPath)-1:]
	if ! h.fileExists(truePath){
		http.NotFound(w,r)
		log.Print("Not Found" + truePath)
		return
	}

	http.ServeFile(w,r,truePath)


}