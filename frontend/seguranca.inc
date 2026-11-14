# Cabeçalhos de segurança. Incluído em cada location porque um add_header na location anula os do server (SEC-02).
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "no-referrer" always;
# style-src 'unsafe-inline': estilos injetados em tempo de execução pelo Angular (inlineCritical desligado no angular.json: o onload inline do CSS crítico violaria script-src).
# Sem unsafe-eval; script só 'self'; connect 'self' (a API passa por /api na mesma origem).
add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'" always;
