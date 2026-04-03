import sys, json, subprocess, urllib.request

url = "http://localhost:18443/api/vehicles?pageSize=50&pageNumber=1"
req = urllib.request.Request(url, headers={"Accept": "application/json"})
with urllib.request.urlopen(req) as resp:
    data = json.loads(resp.read())

vehicles = data.get('vehicles', [])
total = data.get('totalCount', data.get('total', len(vehicles)))
print(f'=== TOTAL VEHICLES: {total} ===\n')

bugs = []
for v in vehicles:
    vid = v.get('id','')[:8]
    title = v.get('title','')
    fuel = v.get('fuelType','')
    trans = v.get('transmission','')
    city = v.get('city','')
    state = v.get('state','')
    price = v.get('price',0)
    munit = v.get('mileageUnit','')
    cond = v.get('condition','')
    body = v.get('bodyStyle','')
    country = v.get('country','')

    english_fuels = ['Gasoline', 'Diesel', 'Electric', 'Hybrid', 'GasElectric']
    english_trans = ['Automatic', 'Manual']

    if fuel in english_fuels:
        bugs.append(f'[{vid}] {title} → fuelType EN: "{fuel}"')
    if trans in english_trans:
        bugs.append(f'[{vid}] {title} → transmission EN: "{trans}"')
    if price <= 0:
        bugs.append(f'[{vid}] {title} → PRECIO SOSPECHOSO: {price}')
    if any(kw in title.lower() for kw in ['e2e', 'test', 'mm8', 'demo']):
        bugs.append(f'[{vid}] {title} → TEST VEHICLE!')
    if cond in ['CertifiedPreOwned', 'New', 'Used']:
        bugs.append(f'[{vid}] {title} → condition EN: "{cond}"')
    if country == 'Dominican Republic':
        bugs.append(f'[{vid}] {title} → country EN: "{country}" (debe ser Republica Dominicana)')

    print(f'  [{vid}] {title} | fuel:{fuel} | trans:{trans} | loc:{city},{state} | price:{price} | cond:{cond} | mileageUnit:{munit}')

print(f'\n=== BUGS ENCONTRADOS ({len(bugs)}) ===')
for b in bugs:
    print(b)
