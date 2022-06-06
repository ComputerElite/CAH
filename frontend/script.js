function  FormatCardSet(set) {
    return `<div style="border-radius: 5px; background-color: #222222BB; padding: 10px; text-align: left;">
                <h3 style="margin-bottom: 0px; margin-top: 0;">${set.name}</h3>
                <div style="margin-top: 0; margin-left: 20px;">by ${set.editors.map(x => x.nickname).join(", ")}</div>
                <div style="margin-top: 10px; font-size: 1.2em;">${set.description.replace(/\\n/g, "<br>")}</div>
                <div style="margin-top: 10px; font-size: 1.2em;">${set.black.length} Questions, ${set.white.length} answers</div>
                <input onclick="SelectSet('${set.name}')" type="button" value="Select">
            </div>`
}

function FormatCard(card, isWhite = false, addRemoveButton = true, showButton = true, org = undefined, label = "Select", winner = false, allSelected = false) {
    if(!org) org = card
    return `<div style="position: relative;">
                <div class="card" style="background-color: #${isWhite ? (winner ? 'FFFF00' : (allSelected ? '999999' : 'FFFFFF')) : '000000'}; color: #${isWhite ? '000000' : 'FFFFFF'};">
                    <div style="margin-top: 10px; font-size: 1.2em;">${card.content.replace(/\\n/g, "<br>")}</div>
                    ${addRemoveButton ? `<input type="button" value="Remove" onclick='Remove${isWhite ? "White" : "Black" }(${JSON.stringify(card).replace(/\'/g, "\\g")})'>` : ``}
                    ${!addRemoveButton && showButton ? `<input type="button" value="${label}" onclick='Select(${JSON.stringify(org).replace(/\'/g, "\\g")})'>` : ``}
                </div>
            </div>`
}

function FormatEditor(editor, method = "Editor") {
    return `<div style="border-radius: 5px; background-color: #000000; padding: 10px; text-align: left; color: #00000;">
                <div style="margin-top: 10px; font-size: 1.2em;">${editor.nickname}</div>
                <input type="button" value="Remove" onclick="Remove${method}('${editor.nickname}')">
            </div>`
}

function IsLoggedIn() {
    if(localStorage.token) return true
    return false
}

function GetUserInfo() {
    return jfetch("/api/v1/me")
}

function TextBoxError(id, text) {
    ChangeTextBoxProperty(id, "var(--red)", text)
}

function TextBoxText(id, text) {
    ChangeTextBoxProperty(id, "var(--highlightedColor)", text)
}

function TextBoxGood(id, text) {
    ChangeTextBoxProperty(id, "var(--textColor)", text)
}

function HideTextBox(id) {
    document.getElementById(id).style.visibility = "hidden"
}

function ChangeTextBoxProperty(id, color, innerHtml) {
    var text = document.getElementById(id)
    text.style.visibility = "visible"
    text.style.border = color + " 1px solid"
    text.innerHTML = innerHtml
}

function tfetch(url, method = "GET", body = "") {
    return ifetch(url, false, method, body)
}

function jfetch(url, method = "GET", body = "") {
    return ifetch(url, true, method, body)
}

function ifetch(url, asjson = true, method = "GET", body = "") {
    return new Promise((resolve, reject) => {
        if(method == "GET" || method == "HEAD") {
            fetch(url, {
                method: method,
                headers: {
                    "token": localStorage.token
                }
            }).then(res => {
                res.text().then(res => {
                    if(asjson) {
                        try {
                            resolve(JSON.parse(res))
                        } catch(e) {
                            reject(e)
                        }
                    } else {
                        resolve(res)
                    }
                })
            })
        } else {
            fetch(url, {
                method: method,
                body: body,
                headers: {
                    "token": localStorage.token
                }
            }).then(res => {
                res.text().then(res => {
                    if(asjson) {
                        try {
                            resolve(JSON.parse(res))
                        } catch(e) {
                            reject(e)
                        }
                    } else {
                        resolve(res)
                    }
                })
            })
        }
    })
}