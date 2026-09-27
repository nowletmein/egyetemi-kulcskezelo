# 1. fázis: Alkalmazás felépítése (Build stage)
FROM node:20-alpine AS build
WORKDIR /app

# Függőségek másolása és telepítése
COPY package*.json ./
RUN npm ci

# Forráskód másolása és a production build elkészítése
COPY . .
RUN npm run build

# 2. fázis: Kiszolgálás Nginx-szel (Production stage)
FROM nginx:alpine AS final

# A lefordított statikus fájlok átmásolása az Nginx könyvtárába
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx konfiguráció (opcionális, ha szükséges) és 80-as port megnyitása
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]