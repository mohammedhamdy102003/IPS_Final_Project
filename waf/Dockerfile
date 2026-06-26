FROM owasp/modsecurity-crs:nginx-alpine
USER root
COPY nginx.conf /etc/nginx/nginx.conf
RUN chown -R nginx:nginx /etc/nginx/nginx.conf
EXPOSE 8080