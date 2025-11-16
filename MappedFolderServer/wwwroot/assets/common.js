let me_data = null
function whoAmI() {
    fetch("/api/v1/users/me/db").then(res => {
        if(!res.ok) {
            showMe()
            return
        }
        res.json().then(data => {
            me_data = data
            showMe()
        })
    })
}

function showMe() {
    const me = document.getElementById("me")
    if(!me_data) {
        me.innerHTML = `<p>You're not logged in</p><a href="/api/v1/sso/startlogin"><button>Log in now</button></a>`
        return;
    }
    for(const e of document.getElementsByClassName("adminOnly")) {
        e.style.display = me_data.isAdmin ? "block" : "none"
    }

    me.innerHTML = `<p>Logged in as: ${me_data.name}<br>IsAdmin: ${me_data.isAdmin}</p><br>
    <a href="/api/v1/sso/signout"><button>Log out</button></a>`
}

function getHost() {
    return location.href.substring(0, location.href.indexOf(location.pathname))
}

setupPopups()
function setupPopups() {
    for(const e of document.getElementsByClassName("popup")) {
        e.onclick = (event) => {
            if(event.target.id !== e.id) return;
            e.style.display = "none"
        }
    }
}