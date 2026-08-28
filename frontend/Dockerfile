# --- Build du bundle statique ---
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# En autohebergement le frontend et l'API sont servis par le meme nginx :
# une URL relative (/api/v1) evite d'avoir a connaitre le nom d'hote public
# au moment du build.
ARG VITE_API_BASE=/api/v1
ENV VITE_API_BASE=$VITE_API_BASE
RUN npm run build

# --- Service statique + reverse-proxy vers l'API ---
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
